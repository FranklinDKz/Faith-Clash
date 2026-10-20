# Monta o jogador a partir do personagem do Mixamo (Ch06): tira cabeca/bone/fones, coloca a cabeca do
# FaceBuilder (rosto das fotos) com cabelo cacheado modelado, sobrancelhas e barba separados, e ajusta o tom
# de pele. Exporta o FBX (esqueleto Humanoid do Mixamo) e as texturas reduzidas.
#
# Uso:  blender -b -P gerar_jogador.py -- <Ch06_nonPBR.fbx> <pasta_saida>
# Env:  FC_FB_BLEND (Cabeca_FaceBuilder.blend)  FC_FB_TEX (Rosto_FaceBuilder.png)
import bpy, bmesh, math, os, sys, random
import numpy as np
from mathutils import Vector, noise

args = sys.argv[sys.argv.index("--") + 1:]
FBX_ENTRADA, SAIDA = args[0], args[1]
FB_BLEND, FB_TEX = os.environ["FC_FB_BLEND"], os.environ["FC_FB_TEX"]
os.makedirs(SAIDA, exist_ok=True)
rnd = random.Random(21)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=FBX_ENTRADA)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
corpo = next(o for o in bpy.data.objects if o.type == "MESH")
corpo.name = "Corpo"
P = "mixamorig9:"

# ------------------------------------------------------------ tira a cabeca (rosto, bone, fones, cabelo)
me = corpo.data
nome_grupo = {g.index: g.name for g in corpo.vertex_groups}
dom = []
for v in me.vertices:
    dom.append(nome_grupo[max(v.groups, key=lambda g: g.weight).group] if v.groups else "")
eh_cabeca = np.array([d in (P + "Head", P + "HeadTop_End") for d in dom])
bm = bmesh.new()
bm.from_mesh(me)
bm.verts.ensure_lookup_table()
apagar = [f for f in bm.faces if all(eh_cabeca[v.index] for v in f.verts)]
bmesh.ops.delete(bm, geom=apagar, context="FACES")
sobra = [v for v in bm.verts if not v.link_faces]
bmesh.ops.delete(bm, geom=sobra, context="VERTS")
bm.to_mesh(me)
bm.free()
me.update()
print("CABECA REMOVIDA faces:", len(apagar), "restam", len(me.polygons))

# material do corpo: pele do corpo igual a do rosto (so tom, as roupas ficam como estao)
bone_head = arm.data.bones[P + "Head"]
z_head = (arm.matrix_world @ bone_head.head_local).z
Z_QUEIXO = float(os.environ.get("FC_Z", "1.59"))

# ------------------------------------------------------------ cabeca do FaceBuilder (reaproveita o gerador do protagonista)
fonte = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gerar_protagonista.py"), encoding="utf-8").read()
ini = fonte.index("def cabeca_facebuilder():")
fim = fonte.index("cabeca_objs = []")
codigo = fonte[ini:fim]
# adaptacoes: altura do queixo e centro (y) de acordo com este esqueleto
codigo = codigo.replace("co[:, 2] += 1.525 - z_queixo", "co[:, 2] += Z_QUEIXO - z_queixo")
codigo = codigo.replace("co[:, 1] -= cy + 0.005", "co[:, 1] -= cy - Y_CENTRO")
codigo = codigo.replace("zc = 1.525", "zc = Z_QUEIXO")
codigo = codigo.replace("reg_barba = escuro * ((t < 0.47) & (yrel < 0.6)).astype(float)", "reg_barba = escuro_b * ((t < 0.30) & (t > -0.01) & (yrel < 0.5) & (ax < 0.05)).astype(float)")
codigo = codigo.replace("escuro = suaviza(escuro, 2)", "escuro_b = suaviza((lum < 0.2).astype(float), 2)\n    escuro = suaviza(escuro, 2)")
codigo = codigo.replace("co *= 0.168 / largura", "co *= LARGURA_CABECA / largura")

col = bpy.data.collections.new("Jogador")
bpy.context.scene.collection.children.link(col)


def link(o):
    col.objects.link(o)
    return o


def mat(nome, rgb, rough=0.5):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    m.diffuse_color = (*rgb, 1)
    return m


def mat_tex(nome, arquivo):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    nt = m.node_tree
    t = nt.nodes.new("ShaderNodeTexImage")
    t.image = bpy.data.images.load(arquivo)
    nt.links.new(t.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    return m


M = {"Pele": mat("Pele", (0.3, 0.13, 0.08), 0.6), "Rosto": mat_tex("Rosto", FB_TEX), "Cabelo": mat("Cabelo", (0.012, 0.011, 0.012), 0.42)}
SLOTS = list(M.keys())
IDX = {n: i for i, n in enumerate(SLOTS)}
# o gerador espera estes nomes; so usa 'arm' para ligar o modificador
Z_QUEIXO_G = Z_QUEIXO
LARGURA_CABECA = float(os.environ.get("FC_LARGURA", "0.155"))
Y_CENTRO = float(os.environ.get("FC_Y", "0.012"))
FB_TEX_G = FB_TEX
ns = dict(globals())
ns.update(Z_QUEIXO=Z_QUEIXO, LARGURA_CABECA=LARGURA_CABECA, Y_CENTRO=Y_CENTRO)
exec(codigo, ns)
objs = ns["cabeca_facebuilder"]()

# os objetos da cabeca foram criados em coordenadas de mundo: pendura no esqueleto preservando a posicao
for ob in objs:
    ob.parent = arm
    ob.matrix_parent_inverse = arm.matrix_world.inverted()
    for g in list(ob.vertex_groups):
        if g.name == "Head":
            g.name = P + "Head"
print("CABECA objetos:", [(o.name, len(o.data.vertices)) for o in objs])

# ------------------------------------------------------------ texturas (reduzidas) e tom de pele
ALVO_PELE = np.array([0.60, 0.41, 0.32])   # tom de pele do rosto (sRGB)


def ajusta_pele(img):
    """Troca o tom da pele do corpo (maos, pescoco) para o tom do rosto; roupas ficam intactas."""
    w, h = img.size
    px = np.zeros(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    px = px.reshape(-1, 4)
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    pele = (r > g) & (g > b) & ((r - b) > 0.10) & (r > 0.18) & (r < 0.8) & (g / np.maximum(r, 1e-4) > 0.45) & (g / np.maximum(r, 1e-4) < 0.85)
    if pele.sum() > 1000:
        atual = px[pele, :3].mean(axis=0)
        fator = ALVO_PELE / np.maximum(atual, 1e-4)
        px[pele, :3] = np.clip(px[pele, :3] * fator, 0, 1)
        print("PELE ajustada", int(pele.sum()), "px", atual.round(3), "->", ALVO_PELE)
    img.pixels.foreach_set(px.ravel())
    img.update()


def salva(img, caminho, tam):
    if img.size[0] > tam:
        img = img.copy()
        img.scale(tam, tam)
    if "Diffuse" in os.path.basename(caminho) and "1001" in caminho:
        ajusta_pele(img)
    img.filepath_raw = caminho
    img.file_format = "PNG"
    img.save()


ts = {}
for m in corpo.data.materials:
    for n in m.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            lig = n.outputs["Color"].links
            alvo = lig[0].to_socket.name if lig else ""
            ts[(m.name, n.image.name)] = (n.image, alvo)
print("TEXTURAS", [(k, v[1]) for k, v in ts.items()])
vistos = set()
for (mn, inome), (img, alvo) in ts.items():
    base = os.path.basename(img.filepath).replace("\\", "/").split("/")[-1]
    base = base.split("\\")[-1]
    if base in vistos:
        continue
    vistos.add(base)
    salva(img, os.path.join(SAIDA, base), 2048)
print("TEXTURAS SALVAS", sorted(vistos))

# ------------------------------------------------------------ exporta
for o in bpy.data.objects:
    o.select_set(False)
exportar = [arm, corpo] + objs
for o in exportar:
    o.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=os.path.join(SAIDA, "Jogador.fbx"), use_selection=True, object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False, bake_anim=False, apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z", axis_up="Y", path_mode="STRIP", embed_textures=False,
)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(os.path.abspath(__file__)), "Jogador.blend"))
print("FBX OK")
