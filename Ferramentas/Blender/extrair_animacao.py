# Extrai so a animacao de um FBX do Mixamo (que vem com a malha, ~55 MB) para um FBX leve (so esqueleto).
# Uso: blender -b -P extrair_animacao.py -- <entrada.fbx> <saida.fbx> <nome_do_clipe>
import bpy, sys

entrada, saida, nome = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=entrada)

for o in list(bpy.data.objects):
    if o.type == "MESH":
        bpy.data.objects.remove(o, do_unlink=True)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
act = arm.animation_data.action
act.name = nome
print("CLIPE", nome, "frames", act.frame_range[:], "fps", bpy.context.scene.render.fps)
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(
    filepath=saida, use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=1.0,
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", path_mode="STRIP",
)
print("OK", saida)
