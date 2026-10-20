# Gera o protagonista do Faith Clash (malha + esqueleto Humanoid + animacoes) e exporta em FBX.
# Uso: blender -b -P gerar_protagonista.py -- <saida.fbx> [<saida.blend>] [<pasta_previews>]
import bpy, bmesh, math, random, sys, os
from mathutils import Vector, Euler

args = sys.argv[sys.argv.index("--") + 1:]
OUT_FBX = args[0]
OUT_BLEND = args[1] if len(args) > 1 else None
PREVIEW_DIR = args[2] if len(args) > 2 else None
random.seed(11)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 24
col = bpy.data.collections.new("Protagonista")
scene.collection.children.link(col)


def link(o):
    col.objects.link(o)
    return o


def select_only(*objs):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[-1]


# ---------------------------------------------------------------- materiais
def mat(nome, rgb, rough=0.55, metal=0.0):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*rgb, 1)
    return m


M = {
    "Pele": mat("Pele", (0.56, 0.37, 0.26), 0.6),
    "Cabelo": mat("Cabelo", (0.02, 0.018, 0.02), 0.5),
    "Olho": mat("Olho", (0.9, 0.88, 0.85), 0.2),
    "Iris": mat("Iris", (0.22, 0.1, 0.05), 0.2),
    "Labios": mat("Labios", (0.62, 0.3, 0.28), 0.5),
    "Camisa": mat("Camisa", (0.05, 0.05, 0.06), 0.8),
    "Calca": mat("Calca", (0.16, 0.17, 0.2), 0.8),
    "Bota": mat("Bota", (0.03, 0.03, 0.03), 0.6),
    "Aco": mat("Aco", (0.78, 0.8, 0.85), 0.25, 1.0),
    "Cabo": mat("Cabo", (0.45, 0.12, 0.1), 0.7),
    "Ouro": mat("Ouro", (0.85, 0.65, 0.2), 0.3, 1.0),
}
SLOTS = list(M.keys())
IDX = {n: i for i, n in enumerate(SLOTS)}

# ---------------------------------------------------------------- corpo (modificador Skin)
nomes, pos, rad, edges = [], [], [], []


def V(n, p, r, pai=None):
    nomes.append(n)
    pos.append(p)
    rad.append(r)
    i = len(nomes) - 1
    if pai is not None:
        edges.append((nomes.index(pai), i))
    return i


V("bacia", (0, 0, 0.95), (0.155, 0.105))
V("cintura", (0, 0, 1.1), (0.145, 0.1), "bacia")
V("peito", (0, 0, 1.28), (0.18, 0.115), "cintura")
V("ombros", (0, 0, 1.4), (0.19, 0.105), "peito")
V("pescoco", (0, 0, 1.5), (0.052, 0.052), "ombros")
for s, sx in (("E", 1), ("D", -1)):
    V("ombro" + s, (0.19 * sx, 0, 1.415), (0.06, 0.06), "ombros")
    V("cotovelo" + s, (0.356 * sx, 0, 1.182), (0.043, 0.043), "ombro" + s)
    V("punho" + s, (0.505 * sx, 0, 0.969), (0.032, 0.032), "cotovelo" + s)
    V("mao" + s, (0.552 * sx, 0, 0.9), (0.04, 0.022), "punho" + s)
    V("quadril" + s, (0.09 * sx, 0, 0.92), (0.088, 0.09), "bacia")
    V("joelho" + s, (0.09 * sx, 0, 0.5), (0.058, 0.06), "quadril" + s)
    V("tornozelo" + s, (0.09 * sx, 0.01, 0.1), (0.046, 0.046), "joelho" + s)
    V("pe" + s, (0.09 * sx, -0.1, 0.045), (0.04, 0.038), "tornozelo" + s)
    V("ponta" + s, (0.09 * sx, -0.19, 0.04), (0.036, 0.028), "pe" + s)

mesh = bpy.data.meshes.new("Corpo")
mesh.from_pydata(pos, edges, [])
corpo = link(bpy.data.objects.new("Corpo", mesh))
select_only(corpo)
bpy.ops.mesh.customdata_skin_add()
sv = mesh.skin_vertices[0].data
for i, r in enumerate(rad):
    sv[i].radius = r
sv[0].use_root = True
sk = corpo.modifiers.new("Skin", "SKIN")
sk.use_smooth_shade = True
sk.branch_smoothing = 0.6
sub = corpo.modifiers.new("Sub", "SUBSURF")
sub.levels = 2
sub.render_levels = 2
bpy.ops.object.modifier_apply(modifier="Skin")
bpy.ops.object.modifier_apply(modifier="Sub")
bpy.ops.object.shade_smooth()
for m in M.values():
    pass
for n in SLOTS:
    corpo.data.materials.append(M[n])
for poly in corpo.data.polygons:
    c = poly.center
    if c.z < 0.075:
        n = "Bota"
    elif c.z < 0.16 and abs(c.x) < 0.2:
        n = "Bota"
    elif c.z < 1.03 and abs(c.x) < 0.2:
        n = "Calca"
    elif 1.03 <= c.z < 1.46 and abs(c.x) < 0.215:
        n = "Camisa"
    else:
        n = "Pele"
    poly.material_index = IDX[n]

# ---------------------------------------------------------------- esqueleto
adata = bpy.data.armatures.new("Esqueleto")
arm = link(bpy.data.objects.new("Esqueleto", adata))
select_only(arm)
bpy.ops.object.mode_set(mode="EDIT")
eb = {}


def bone(nome, h, t, pai=None):
    b = adata.edit_bones.new(nome)
    b.head, b.tail = Vector(h), Vector(t)
    if pai:
        b.parent = eb[pai]
    eb[nome] = b


bone("Hips", (0, 0, 0.95), (0, 0, 1.05))
bone("Spine", (0, 0, 1.05), (0, 0, 1.2), "Hips")
bone("Chest", (0, 0, 1.2), (0, 0, 1.38), "Spine")
bone("Neck", (0, 0, 1.38), (0, 0, 1.52), "Chest")
bone("Head", (0, 0, 1.52), (0, 0, 1.76), "Neck")
for lado, sx in (("Left", 1), ("Right", -1)):
    d = Vector((0.574 * sx, 0, -0.819))
    sh = Vector((0.19 * sx, 0, 1.415))
    el = sh + d * 0.29
    wr = el + d * 0.26
    hd = wr + d * 0.09
    bone(lado + "Shoulder", (0.03 * sx, 0, 1.4), sh, "Chest")
    bone(lado + "UpperArm", sh, el, lado + "Shoulder")
    bone(lado + "LowerArm", el, wr, lado + "UpperArm")
    bone(lado + "Hand", wr, hd, lado + "LowerArm")
    bone(lado + "UpperLeg", (0.09 * sx, 0, 0.92), (0.09 * sx, 0, 0.5), "Hips")
    bone(lado + "LowerLeg", (0.09 * sx, 0, 0.5), (0.09 * sx, 0.01, 0.1), lado + "UpperLeg")
    bone(lado + "Foot", (0.09 * sx, 0.01, 0.1), (0.09 * sx, -0.12, 0.04), lado + "LowerLeg")
    bone(lado + "Toes", (0.09 * sx, -0.12, 0.04), (0.09 * sx, -0.2, 0.04), lado + "Foot")
bpy.ops.object.mode_set(mode="OBJECT")

# pesos automaticos no corpo
select_only(corpo, arm)
bpy.ops.object.parent_set(type="ARMATURE_AUTO")

# ---------------------------------------------------------------- cabeca, rosto, cabelo e katana (rigidos)
partes = []  # (nome_material, bmesh criado)


def esfera(mat_n, centro, escala, seg=16, rings=10, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    bmesh.ops.scale(bm, vec=escala, verts=bm.verts)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Euler([math.radians(a) for a in rot]).to_matrix())
    bmesh.ops.translate(bm, vec=centro, verts=bm.verts)
    partes.append((mat_n, bm, "Head"))


def caixa(mat_n, centro, tam, grupo, rot=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=tam, verts=bm.verts)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Euler([math.radians(a) for a in rot]).to_matrix())
    bmesh.ops.translate(bm, vec=centro, verts=bm.verts)
    partes.append((mat_n, bm, grupo))


HC = (0, 0.0, 1.63)
esfera("Pele", HC, (0.077, 0.095, 0.108))                       # cranio
esfera("Pele", (0, -0.022, 1.565), (0.064, 0.07, 0.07))          # mandibula
esfera("Pele", (0, -0.098, 1.6), (0.013, 0.019, 0.023), 10, 8)    # nariz
esfera("Pele", (0, -0.092, 1.583), (0.021, 0.012, 0.01), 10, 8)   # ponta do nariz
for sx in (1, -1):
    esfera("Olho", (0.032 * sx, -0.083, 1.642), (0.0165, 0.012, 0.012), 10, 8)
    esfera("Iris", (0.032 * sx, -0.094, 1.642), (0.009, 0.004, 0.009), 10, 8)
    caixa("Cabelo", (0.034 * sx, -0.088, 1.67), (0.034, 0.01, 0.008), "Head", (0, 0, 8 * sx))  # sobrancelha
    esfera("Pele", (0.081 * sx, 0.005, 1.62), (0.008, 0.016, 0.027), 8, 6)  # orelha
esfera("Labios", (0, -0.087, 1.552), (0.025, 0.01, 0.007), 10, 8)
caixa("Cabelo", (0, -0.092, 1.568), (0.045, 0.008, 0.01), "Head")          # bigode
esfera("Cabelo", (0, -0.072, 1.512), (0.03, 0.02, 0.026), 10, 8)          # cavanhaque
esfera("Cabelo", (0, 0.015, 1.69), (0.083, 0.098, 0.078), 16, 10)         # base do cabelo
for _ in range(110):                                                      # cachos
    a = random.uniform(0, 2 * math.pi)
    e = math.acos(random.uniform(0.15, 1.0))
    rx, ry, rz = 0.088, 0.1, 0.085
    x = rx * math.sin(e) * math.cos(a)
    y = 0.015 + ry * math.sin(e) * math.sin(a)
    z = 1.69 + rz * math.cos(e)
    if y < -0.06 and z < 1.7:
        continue
    r = random.uniform(0.019, 0.028)
    esfera("Cabelo", (x, y, z), (r, r, r), 8, 6)
for _ in range(14):                                                       # franja
    x = random.uniform(-0.055, 0.055)
    esfera("Cabelo", (x, random.uniform(-0.082, -0.06), random.uniform(1.715, 1.745)), (0.022, 0.02, 0.02), 8, 6)

# katana presa a mao direita (peso rigido no osso RightHand)
dir_d = Vector((-0.574, 0, -0.819))
ref = Vector((-0.505, 0, 0.969))


def ponto(t):
    return ref + dir_d * t


def cilindro(mat_n, t0, t1, raio_xy, grupo="RightHand", achatar=1.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=raio_xy, radius2=raio_xy, depth=(t1 - t0))
    for v in bm.verts:
        v.co.x *= achatar
    # eixo do cone e Z; alinhar a dir_d
    q = Vector((0, 0, 1)).rotation_difference(dir_d)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=q.to_matrix())
    bmesh.ops.translate(bm, vec=(ponto((t0 + t1) / 2)), verts=bm.verts)
    partes.append((mat_n, bm, grupo))


cilindro("Cabo", -0.06, 0.2, 0.017)                    # cabo (tsuka)
cilindro("Ouro", 0.2, 0.215, 0.034, achatar=0.4)       # guarda (tsuba)
cilindro("Aco", 0.215, 1.05, 0.016, achatar=0.22)      # lamina

# juntar partes rigidas em um unico mesh
hmesh = bpy.data.meshes.new("Detalhes")
bm_all = bmesh.new()
mat_idx_layer = []
for mat_n, bm, grupo in partes:
    for f in bm.faces:
        f.material_index = IDX[mat_n]
    tmp = bpy.data.meshes.new("t")
    bm.to_mesh(tmp)
    bm.free()
    bm_all.from_mesh(tmp)
    mat_idx_layer.append((grupo, len(tmp.vertices)))
    bpy.data.meshes.remove(tmp)
bm_all.to_mesh(hmesh)
bm_all.free()
for n in SLOTS:
    hmesh.materials.append(M[n])
detalhes = link(bpy.data.objects.new("Detalhes", hmesh))
ini = 0
grupos = {}
for g, n in mat_idx_layer:
    grupos.setdefault(g, []).extend(range(ini, ini + n))
    ini += n
for g, idxs in grupos.items():
    vg = detalhes.vertex_groups.new(name=g)
    vg.add(idxs, 1.0, "REPLACE")
md = detalhes.modifiers.new("Armature", "ARMATURE")
md.object = arm
detalhes.parent = arm
select_only(detalhes)
bpy.ops.object.shade_smooth()

# juntar tudo em um mesh so
select_only(detalhes, corpo)
bpy.ops.object.join()
personagem = bpy.context.view_layer.objects.active
personagem.name = "Protagonista"
personagem.data.name = "Protagonista"

# ---------------------------------------------------------------- animacao
arm.animation_data_create()
pb = arm.pose.bones
rest = {b.name: b.matrix_local.to_3x3() for b in adata.bones}
for p in pb:
    p.rotation_mode = "QUATERNION"


def set_rot(nome, rx, ry, rz):
    W = Euler((math.radians(rx), math.radians(ry), math.radians(rz)), "XYZ").to_matrix()
    R = rest[nome]
    pb[nome].rotation_quaternion = (R.inverted() @ W @ R).to_quaternion()


def set_hips_loc(x, y, z):
    pb["Hips"].location = rest["Hips"].inverted() @ Vector((x, y, z))


def ss(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def mirror(d):
    """Aceita chaves 'LUA' (Left/Right UpperArm etc.) com valores (rx,ry,rz) e devolve pose espelhada se for 'LR'."""
    return d


def extent(ul, ll):
    t1 = math.radians(-ul)
    t2 = math.radians(-ul + ll)  # joelho dobra a canela para tras (+X)
    return 0.42 * math.cos(t1) + 0.42 * math.cos(t2)


def grounded(p):
    """Calcula o deslocamento vertical do quadril para o pe mais baixo tocar o chao."""
    e = max(extent(p.get("LeftUpperLeg", (0, 0, 0))[0], p.get("LeftLowerLeg", (0, 0, 0))[0]),
            extent(p.get("RightUpperLeg", (0, 0, 0))[0], p.get("RightLowerLeg", (0, 0, 0))[0]))
    return e - 0.84


def auto_arms(p):
    """Antebracos giram para dentro conforme flexionam (a pose-A abre os bracos)."""
    for s, sg in (("Left", 1), ("Right", -1)):
        la = p.get(s + "LowerArm", (0, 0, 0))
        if la[2] == 0:
            p[s + "LowerArm"] = (la[0], la[1], -sg * 0.5 * abs(la[0]))
    return p


def auto_feet(p):
    for s in ("Left", "Right"):
        ul = p.get(s + "UpperLeg", (0, 0, 0))[0]
        ll = p.get(s + "LowerLeg", (0, 0, 0))[0]
        if s + "Foot" not in p:
            p[s + "Foot"] = (-(ul - ll) * 0.9, 0, 0)  # mantem o pe mais ou menos paralelo ao chao
    return p


def pose_from_keys(keys, t):
    """keys: lista de (t, pose_dict). Interpola suavemente entre as poses."""
    keys = sorted(keys, key=lambda k: k[0])
    if t <= keys[0][0]:
        return dict(keys[0][1])
    if t >= keys[-1][0]:
        return dict(keys[-1][1])
    for (t0, a), (t1, b) in zip(keys, keys[1:]):
        if t0 <= t <= t1:
            u = ss((t - t0) / (t1 - t0))
            out = {}
            for n in set(a) | set(b):
                va, vb = a.get(n, (0, 0, 0)), b.get(n, (0, 0, 0))
                out[n] = tuple(va[i] + (vb[i] - va[i]) * u for i in range(3))
            return out


def aplicar(p, hips=(0, 0, 0), ground=True):
    for b in pb:
        b.rotation_quaternion = (1, 0, 0, 0)
        b.location = (0, 0, 0)
    auto_feet(p)
    auto_arms(p)
    for n, v in p.items():
        set_rot(n, *v)
    set_hips_loc(*hips)
    if ground:
        bpy.context.view_layer.update()
        baixo = min(pb["LeftFoot"].tail.z, pb["RightFoot"].tail.z, pb["LeftToes"].tail.z, pb["RightToes"].tail.z,
                    pb["LeftFoot"].head.z - 0.06, pb["RightFoot"].head.z - 0.06)
        set_hips_loc(hips[0], hips[1], hips[2] + (0.04 - baixo))


def gravar(nome, frames, fn, loop):
    act = bpy.data.actions.new(nome)
    act.use_fake_user = True
    arm.animation_data.action = act
    for i in range(frames + 1):
        t = i / frames
        pose, hips, ground = fn(t)
        aplicar(pose, hips, ground)
        f = i + 1
        for b in pb:
            b.keyframe_insert("rotation_quaternion", frame=f)
        pb["Hips"].keyframe_insert("location", frame=f)
    act.use_frame_range = True
    act.frame_start, act.frame_end = 1, frames + 1
    act.use_cyclic = loop


def bracos(ua_l, la_l, ua_r, la_r, ry=(0, 0)):
    return {
        "LeftUpperArm": (ua_l[0], ry[0] if len(ua_l) < 2 else ua_l[1], 0), "LeftLowerArm": (la_l, 0, 0),
        "RightUpperArm": (ua_r[0], ry[1] if len(ua_r) < 2 else ua_r[1], 0), "RightLowerArm": (la_r, 0, 0),
    }


# pose base de guarda (luta): joelhos flexionados, bracos recolhidos
GUARDA = {
    "Spine": (4, 0, 0), "Chest": (3, 0, 0), "Head": (-5, 0, 0), "Hips": (0, 0, -12), "Chest ": (0, 0, 0),
    "LeftUpperLeg": (-16, 0, 0), "LeftLowerLeg": (24, 0, 0),
    "RightUpperLeg": (12, 0, 0), "RightLowerLeg": (26, 0, 0),
    "LeftUpperArm": (-45, 26, 0), "LeftLowerArm": (-95, 0, 0),
    "RightUpperArm": (-40, -26, 0), "RightLowerArm": (-68, 0, 0),
}
GUARDA.pop("Chest ")


def mix(a, b, u):
    out = {}
    for n in set(a) | set(b):
        va, vb = a.get(n, (0, 0, 0)), b.get(n, (0, 0, 0))
        out[n] = tuple(va[i] + (vb[i] - va[i]) * u for i in range(3))
    return out


def com(base, **mods):
    out = dict(base)
    for k, v in mods.items():
        out[k] = v
    return out


def idle(t):
    s = math.sin(2 * math.pi * t)
    p = dict(GUARDA)
    p["Chest"] = (3 + 1.5 * s, 0, 0)
    p["Spine"] = (4 + 0.8 * s, 0, 0)
    p["LeftUpperArm"] = (-45 + 2 * s, 26, 0)
    p["RightUpperArm"] = (-40 + 2 * s, -26, 0)
    return p, (0, 0, 0.0), True


def andar(t):
    ph = 2 * math.pi * t
    p = {}
    for s, sg in (("Left", 1), ("Right", -1)):
        a = ph if sg == 1 else ph + math.pi
        p[s + "UpperLeg"] = (-26 * math.sin(a), 0, 0)
        p[s + "LowerLeg"] = (6 + 38 * max(0.0, math.cos(a)), 0, 0)
        p[s + "UpperArm"] = (24 * math.sin(a), 30 * sg, 0)
        p[s + "LowerArm"] = (-18 - 8 * max(0.0, -math.sin(a)), 0, 0)
    p["Hips"] = (0, 0, 4 * math.cos(ph))
    p["Spine"] = (2, 0, -3 * math.cos(ph))
    p["Chest"] = (2, 0, -3 * math.cos(ph))
    p["Head"] = (-2, 0, 0)
    return p, (0, 0, 0), True


def correr(t):
    ph = 2 * math.pi * t
    p = {}
    for s, sg in (("Left", 1), ("Right", -1)):
        a = ph if sg == 1 else ph + math.pi
        p[s + "UpperLeg"] = (-52 * math.sin(a) - 8, 0, 0)
        p[s + "LowerLeg"] = (12 + 85 * max(0.0, math.cos(a)), 0, 0)
        p[s + "UpperArm"] = (45 * math.sin(a), 22 * sg, 0)
        p[s + "LowerArm"] = (-85 - 10 * math.sin(a), 0, 0)
    p["Hips"] = (0, 0, 7 * math.cos(ph))
    p["Spine"] = (10, 0, -6 * math.cos(ph))
    p["Chest"] = (8, 0, -6 * math.cos(ph))
    p["Head"] = (-12, 0, 0)
    return p, (0, 0, 0), True


def pular(t):
    keys = [
        (0.0, dict(GUARDA)),
        (0.22, {"Spine": (14, 0, 0), "Chest": (10, 0, 0), "LeftUpperLeg": (-45, 0, 0), "LeftLowerLeg": (80, 0, 0),
                "RightUpperLeg": (-45, 0, 0), "RightLowerLeg": (80, 0, 0),
                "LeftUpperArm": (30, 10, 0), "RightUpperArm": (30, -10, 0),
                "LeftLowerArm": (-20, 0, 0), "RightLowerArm": (-20, 0, 0)}),
        (0.45, {"Spine": (-4, 0, 0), "Chest": (-6, 0, 0), "LeftUpperLeg": (-8, 0, 0), "LeftLowerLeg": (10, 0, 0),
                "RightUpperLeg": (6, 0, 0), "RightLowerLeg": (10, 0, 0),
                "LeftUpperArm": (-120, 0, 0), "RightUpperArm": (-120, 0, 0),
                "LeftLowerArm": (-20, 0, 0), "RightLowerArm": (-20, 0, 0)}),
        (1.0, {"Spine": (8, 0, 0), "Chest": (6, 0, 0), "LeftUpperLeg": (-55, 0, 0), "LeftLowerLeg": (70, 0, 0),
               "RightUpperLeg": (-20, 0, 0), "RightLowerLeg": (45, 0, 0),
               "LeftUpperArm": (-40, 35, 0), "RightUpperArm": (-40, -35, 0),
               "LeftLowerArm": (-60, 0, 0), "RightLowerArm": (-60, 0, 0)}),
    ]
    return pose_from_keys(keys, t), (0, 0, 0), t < 0.3


def esquiva(t):
    tuck = {"Spine": (45, 0, 0), "Chest": (35, 0, 0), "Head": (25, 0, 0),
            "LeftUpperLeg": (-110, 0, 0), "LeftLowerLeg": (130, 0, 0),
            "RightUpperLeg": (-110, 0, 0), "RightLowerLeg": (130, 0, 0),
            "LeftUpperArm": (-70, 20, 0), "RightUpperArm": (-70, -20, 0),
            "LeftLowerArm": (-110, 0, 0), "RightLowerArm": (-110, 0, 0)}
    keys = [(0.0, dict(GUARDA)), (0.18, tuck), (0.8, tuck), (1.0, dict(GUARDA))]
    p = pose_from_keys(keys, t)
    roll = 360 * ss((t - 0.12) / 0.7)
    p["Hips"] = (roll, 0, 0)
    low = 0.5 * math.sin(math.pi * ss((t - 0.05) / 0.9)) ** 0.5
    return p, (0, -0.35 * ss((t - 0.1) / 0.8) + 0.35 * ss((t - 0.1) / 0.8) * 0, -0.38 * low / 0.5 * 0.9), t < 0.12 or t > 0.9


def defesa(t):
    s = math.sin(2 * math.pi * t)
    p = dict(GUARDA)
    p.update({"LeftUpperArm": (-68 + s, 10, 0), "RightUpperArm": (-70 + s, -12, 0),
              "LeftLowerArm": (-105, 0, 0), "RightLowerArm": (-100, 0, 0),
              "Spine": (9, 0, 0), "Chest": (6, 0, 0), "Head": (-8, 0, 0)})
    return p, (0, 0, 0), True


def atk(keys):
    return lambda t: (pose_from_keys(keys, t), (0, 0, 0), True)


G = dict(GUARDA)
# Ataque 1: corte horizontal da direita para a esquerda (impacto em ~37%)
A1 = [
    (0.0, G),
    (0.2, com(G, Chest=(3, 0, -38), Hips=(0, 0, -30), Spine=(4, 0, -10),
              RightUpperArm=(-50, -62, 0), RightLowerArm=(-70, 0, 0), LeftUpperArm=(-40, 30, 0))),
    (0.37, com(G, Chest=(5, 0, 42), Hips=(0, 0, 18), Spine=(6, 0, 14),
               RightUpperArm=(-70, 10, 0), RightLowerArm=(-25, 0, 0), LeftUpperArm=(-40, 30, 0),
               LeftUpperLeg=(-30, 0, 0), LeftLowerLeg=(35, 0, 0))),
    (0.65, com(G, Chest=(5, 0, 36), Hips=(0, 0, 10), RightUpperArm=(-60, 20, 0), RightLowerArm=(-40, 0, 0),
               LeftUpperLeg=(-24, 0, 0), LeftLowerLeg=(30, 0, 0))),
    (1.0, G),
]
# Ataque 2: corte de volta da esquerda para a direita
A2 = [
    (0.0, G),
    (0.2, com(G, Chest=(3, 0, 34), Hips=(0, 0, 8), Spine=(4, 0, 10),
              RightUpperArm=(-55, 30, 0), RightLowerArm=(-45, 0, 0))),
    (0.37, com(G, Chest=(5, 0, -48), Hips=(0, 0, -34), Spine=(6, 0, -14),
               RightUpperArm=(-75, -70, 0), RightLowerArm=(-60, 0, 0),
               LeftUpperLeg=(8, 0, 0), RightUpperLeg=(-28, 0, 0), RightLowerLeg=(35, 0, 0))),
    (0.65, com(G, Chest=(5, 0, -40), Hips=(0, 0, -26), RightUpperArm=(-70, -60, 0), RightLowerArm=(-60, 0, 0))),
    (1.0, G),
]
# Ataque 3: golpe descendente com avanco (impacto em ~42%)
A3 = [
    (0.0, G),
    (0.25, com(G, Spine=(-4, 0, 0), Chest=(-8, 0, 0), RightUpperArm=(-165, -10, 0), RightLowerArm=(-50, 0, 0),
               LeftUpperArm=(-150, 20, 0), LeftLowerArm=(-60, 0, 0))),
    (0.42, com(G, Spine=(20, 0, 0), Chest=(22, 0, 0), Head=(-22, 0, 0), RightUpperArm=(-80, -6, 0),
               RightLowerArm=(-20, 0, 0), LeftUpperArm=(-75, 16, 0), LeftLowerArm=(-25, 0, 0),
               LeftUpperLeg=(-42, 0, 0), LeftLowerLeg=(48, 0, 0), RightUpperLeg=(14, 0, 0))),
    (0.72, com(G, Spine=(16, 0, 0), Chest=(18, 0, 0), RightUpperArm=(-72, -6, 0), RightLowerArm=(-22, 0, 0),
               LeftUpperArm=(-70, 16, 0), LeftLowerArm=(-25, 0, 0), LeftUpperLeg=(-38, 0, 0), LeftLowerLeg=(44, 0, 0))),
    (1.0, G),
]
# Ataque pesado: golpe enorme sobre a cabeca (impacto em ~53%)
AP = [
    (0.0, G),
    (0.35, com(G, Spine=(-10, 0, 0), Chest=(-16, 0, 0), Head=(8, 0, 0), RightUpperArm=(-175, -14, 0),
               RightLowerArm=(-40, 0, 0), LeftUpperArm=(-160, 22, 0), LeftLowerArm=(-50, 0, 0),
               LeftUpperLeg=(-8, 0, 0), RightUpperLeg=(8, 0, 0))),
    (0.53, com(G, Spine=(26, 0, 0), Chest=(28, 0, 0), Head=(-28, 0, 0), RightUpperArm=(-75, -4, 0),
               RightLowerArm=(-18, 0, 0), LeftUpperArm=(-70, 14, 0), LeftLowerArm=(-22, 0, 0),
               LeftUpperLeg=(-55, 0, 0), LeftLowerLeg=(60, 0, 0), RightUpperLeg=(18, 0, 0), RightLowerLeg=(14, 0, 0))),
    (0.8, com(G, Spine=(22, 0, 0), Chest=(24, 0, 0), RightUpperArm=(-72, -4, 0), RightLowerArm=(-20, 0, 0),
              LeftUpperArm=(-70, 14, 0), LeftUpperLeg=(-50, 0, 0), LeftLowerLeg=(56, 0, 0))),
    (1.0, G),
]


def dano(t):
    keys = [(0.0, G), (0.25, com(G, Spine=(-12, 0, 0), Chest=(-14, 0, 0), Head=(18, 0, 0),
                                 LeftUpperArm=(10, 20, 0), RightUpperArm=(10, -20, 0),
                                 LeftLowerArm=(-20, 0, 0), RightLowerArm=(-20, 0, 0))), (1.0, G)]
    return pose_from_keys(keys, t), (0, 0.04 * math.sin(math.pi * t), 0), True


def morte(t):
    keys = [(0.0, G),
            (0.6, {"Spine": (-20, 0, 0), "Chest": (-15, 0, 0), "LeftUpperLeg": (20, 0, 0), "LeftLowerLeg": (90, 0, 0),
                   "RightUpperLeg": (-30, 0, 0), "RightLowerLeg": (70, 0, 0), "LeftUpperArm": (20, 40, 0),
                   "RightUpperArm": (20, -40, 0), "Head": (20, 0, 0)}),
            (1.0, {"Hips": (-90, 0, 0), "LeftUpperLeg": (-5, 0, 0), "RightUpperLeg": (-5, 0, 0),
                   "LeftUpperArm": (0, 35, 0), "RightUpperArm": (0, -35, 0), "Head": (10, 0, 0)})]
    p = pose_from_keys(keys, t)
    return p, (0, 0.5 * ss((t - 0.55) / 0.45), -0.62 * ss((t - 0.55) / 0.45)), t < 0.6


ACOES = [
    ("Parado", 48, idle, True), ("Andar", 24, andar, True), ("Correr", 16, correr, True),
    ("Pular", 24, pular, False), ("Esquiva", 20, esquiva, False), ("Defesa", 24, defesa, True),
    ("Ataque1", 10, atk(A1), False), ("Ataque2", 10, atk(A2), False), ("Ataque3", 14, atk(A3), False),
    ("AtaquePesado", 20, atk(AP), False), ("Dano", 10, dano, False), ("Morte", 36, morte, False),
]
for nome, frames, fn, loop in ACOES:
    gravar(nome, frames, fn, loop)

# ---------------------------------------------------------------- previews (opcional)
if PREVIEW_DIR:
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.light = "STUDIO"
    scene.render.resolution_x, scene.render.resolution_y = 360, 480
    cam_d = bpy.data.cameras.new("cam")
    cam_d.type = "ORTHO"
    cam_d.ortho_scale = 2.3
    cam = bpy.data.objects.new("cam", cam_d)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = (-2.6, -3.4, 1.0)
    cam.rotation_euler = (math.radians(86), 0, math.radians(-37))
    bpy.context.view_layer.update()
    # camera lateral e frontal
    amostras = {"Parado": [1], "Andar": [1, 7], "Correr": [1, 5], "Pular": [1, 7, 12, 25], "Esquiva": [8, 12],
                "Defesa": [1], "Ataque1": [1, 3, 4, 7], "Ataque2": [4], "Ataque3": [4, 6], "AtaquePesado": [8, 11],
                "Dano": [4], "Morte": [37]}
    for nome, fr in amostras.items():
        arm.animation_data.action = bpy.data.actions[nome]
        for f in fr:
            scene.frame_set(f)
            scene.render.filepath = os.path.join(PREVIEW_DIR, f"{nome}_{f:02d}.png")
            bpy.ops.render.render(write_still=True)
    # frente
    cam.location = (0, -4, 1.0)
    cam.rotation_euler = (math.radians(90), 0, 0)
    arm.animation_data.action = bpy.data.actions["Parado"]
    scene.frame_set(1)
    scene.render.filepath = os.path.join(PREVIEW_DIR, "frente.png")
    bpy.ops.render.render(write_still=True)
    # rosto
    cam_d.ortho_scale = 0.5
    cam.location = (0, -2, 1.64)
    scene.render.filepath = os.path.join(PREVIEW_DIR, "rosto.png")
    bpy.ops.render.render(write_still=True)

# ---------------------------------------------------------------- exportar
if OUT_BLEND:
    bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND)
arm.animation_data.action = bpy.data.actions["Parado"]
select_only(personagem, arm)
os.makedirs(os.path.dirname(OUT_FBX), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=OUT_FBX, use_selection=True, object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    primary_bone_axis="Y", secondary_bone_axis="X", path_mode="COPY", embed_textures=False,
)
print("FBX OK:", OUT_FBX)
