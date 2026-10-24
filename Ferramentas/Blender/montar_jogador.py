# Monta o personagem do jogo a partir de assets CC0 da Quaternius:
#   roupa "Ranger" (capuz, peitoral, braços, calça, botas) + cabeça/olhos/sobrancelhas do corpo "Superhero".
# Tudo no MESMO esqueleto (Humanoid na Unity), exportado em um único FBX.
# Uso: blender -b -P montar_jogador.py -- <roupa.fbx> <corpo_superhero.fbx> <saida.fbx> [cabelo.fbx ...]
import bpy, sys
args = sys.argv[sys.argv.index("--") + 1:]
roupa, corpo, saida, extras = args[0], args[1], args[2], args[3:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=roupa)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")

antes = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=corpo)
novos = [o for o in bpy.data.objects if o not in antes]
arm2 = next(o for o in novos if o.type == "ARMATURE")


def reparentar(o):
    o.parent = arm
    for m in o.modifiers:
        if m.type == "ARMATURE":
            m.object = arm


for o in novos:
    if o.type != "MESH":
        continue
    if o.name.startswith("SuperHero"):
        # fica só a cabeça e o pescoço (vértices pesados nos ossos neck_01 / Head)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="DESELECT")
        bpy.ops.object.mode_set(mode="OBJECT")
        grupos = {g.index: g.name for g in o.vertex_groups}
        manter = set()
        for v in o.data.vertices:
            pesos = {grupos[g.group]: g.weight for g in v.groups}
            if pesos.get("Head", 0) + pesos.get("neck_01", 0) > 0.5:
                manter.add(v.index)
        for v in o.data.vertices:
            v.select = v.index not in manter
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.delete(type="VERT")
        bpy.ops.object.mode_set(mode="OBJECT")
        o.name = "Cabeca"
    reparentar(o)
for o in novos:
    if o.type == "ARMATURE":
        bpy.data.objects.remove(o, do_unlink=True)

for e in extras:
    antes = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=e)
    nv = [o for o in bpy.data.objects if o not in antes]
    for o in nv:
        if o.type == "MESH":
            reparentar(o)
    for o in nv:
        if o.type == "ARMATURE":
            bpy.data.objects.remove(o, do_unlink=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=saida, use_selection=True, object_types={"ARMATURE", "MESH"}, add_leaf_bones=False,
                         bake_anim=False, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", path_mode="STRIP")
print("OK", saida, [(o.name, o.type) for o in bpy.data.objects])
