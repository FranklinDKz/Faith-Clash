# Gera a katana (lamina curva, guarda, cabo trancado) e exporta FBX. Origem no centro do cabo; lamina para +Y.
# Uso: blender -b -P gerar_katana.py -- <saida.fbx>
import bpy, bmesh, math, sys
from mathutils import Vector

saida = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(nome, rgb, rough, metal=0.0):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*rgb, 1)
    return m


mats = [mat("Aco", (0.8, 0.82, 0.86), 0.22, 1.0), mat("Cabo", (0.05, 0.03, 0.03), 0.7), mat("Ouro", (0.85, 0.65, 0.2), 0.3, 1.0)]
bm = bmesh.new()
uv = bm.loops.layers.uv.new("UVMap")


def anel(centro, tang, larg, esp, lado):
    """Seccao da lamina (losango achatado) perpendicular a tang."""
    up = Vector((0, 0, 1))
    side = tang.cross(up).normalized()
    pts = [centro + side * larg * 0.5, centro + up * esp * 0.5, centro - side * larg * 0.5 * 0.55, centro - up * esp * 0.5]
    return [bm.verts.new(p) for p in pts]


def liga(a, b, mi):
    for i in range(len(a)):
        f = bm.faces.new((a[i], a[(i + 1) % len(a)], b[(i + 1) % len(a)], b[i]))
        f.material_index = mi
        f.smooth = True


# lamina: 0.72 m com curvatura leve (arco no plano YX), ponta em kissaki
comp, N = 0.72, 18
aneis = []
for i in range(N + 1):
    t = i / N
    y = 0.14 + comp * t
    x = 0.035 * (t ** 2)             # curva
    tang = Vector((0.035 * 2 * t / comp * 0.0 + (0.07 * t) / comp, 1, 0)).normalized()
    larg = 0.031 * (1 - 0.18 * t) if t < 0.93 else 0.031 * (1 - 0.18 * t) * (1 - (t - 0.93) / 0.07)
    esp = 0.007 * (1 - 0.3 * t) if t < 0.93 else 0.007 * (1 - 0.3 * t) * (1 - (t - 0.93) / 0.07)
    aneis.append(anel(Vector((x, y, 0)), tang, max(larg, 0.0008), max(esp, 0.0004), 1))
for i in range(N):
    liga(aneis[i], aneis[i + 1], 0)
cap = bm.faces.new(aneis[0][::-1]); cap.material_index = 0
cap2 = bm.faces.new(aneis[-1]); cap2.material_index = 0

# guarda (tsuba): disco achatado
bmesh.ops.create_cone(bm, cap_ends=True, segments=20, radius1=0.045, radius2=0.045, depth=0.006,
                      matrix=__import__("mathutils").Matrix.Translation((0, 0.138, 0)) @ __import__("mathutils").Matrix.Rotation(math.radians(90), 4, "X"))
# cabo (tsuka) de -0.14 a 0.135
bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.016, radius2=0.017, depth=0.275,
                      matrix=__import__("mathutils").Matrix.Translation((0, -0.002, 0)) @ __import__("mathutils").Matrix.Rotation(math.radians(90), 4, "X"))
# material por posicao
for f in bm.faces:
    c = f.calc_center_median()
    if f.material_index == 0 and c.y < 0.14 and abs(c.x) < 0.05:
        f.material_index = 1
    elif c.y > 0.13 and c.y < 0.146 and abs(c.x) < 0.05 and len(f.verts) > 4:
        f.material_index = 2
for f in bm.faces:
    c = f.calc_center_median()
    if c.y > 0.13 and c.y < 0.146 and (f.calc_area() > 0.0008 or len(f.verts) == 20):
        f.material_index = 2
    elif c.y < 0.136 and f.material_index != 2:
        f.material_index = 1
me = bpy.data.meshes.new("Katana")
bm.to_mesh(me)
bm.free()
for m in mats:
    me.materials.append(m)
ob = bpy.data.objects.new("Katana", me)
bpy.context.scene.collection.objects.link(ob)
bpy.context.view_layer.objects.active = ob
ob.select_set(True)
bpy.ops.object.shade_smooth()
bpy.ops.export_scene.fbx(filepath=saida, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", path_mode="STRIP")
print("KATANA OK", saida)
