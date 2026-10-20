import bpy, sys
saida = sys.argv[sys.argv.index("--") + 1]
for o in list(bpy.data.objects):
    if o.type == "MESH":
        bpy.data.objects.remove(o, do_unlink=True)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True); bpy.context.view_layer.objects.active = arm
bpy.ops.export_scene.fbx(filepath=saida, use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
    apply_scale_options="FBX_SCALE_ALL", axis_forward="Z", axis_up="Y", path_mode="STRIP")
print("BASE OK")
