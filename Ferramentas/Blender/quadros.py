# Renderiza N quadros de uma animacao (vista lateral) com o numero do quadro, para escolher cortes.
# Uso: blender -b -P quadros.py -- <fbx> <saida_prefixo> <n_quadros> [frame_ini frame_fim]
import bpy, sys, math, os
args = sys.argv[sys.argv.index("--") + 1:]
fbx, saida, n = args[0], args[1], int(args[2])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
sc = bpy.context.scene
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
a = arm.animation_data.action
f0, f1 = int(a.frame_range[0]), int(a.frame_range[1])
if len(args) >= 5:
    f0, f1 = int(args[3]), int(args[4])
cd = bpy.data.cameras.new("c"); cd.type = "ORTHO"; cd.ortho_scale = 3.2
cam = bpy.data.objects.new("c", cd); sc.collection.objects.link(cam); sc.camera = cam
sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "OBJECT"
sc.render.resolution_x, sc.render.resolution_y = 170, 220
bpy.context.scene.world = bpy.data.worlds.new("w"); bpy.context.scene.world.color = (0.35, 0.4, 0.45)
bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, 0))
for i in range(n):
    f = f0 + int(i * (f1 - f0) / max(1, n - 1))
    sc.frame_set(f)
    hips = arm.matrix_world @ arm.pose.bones["mixamorig9:Hips"].head
    cam.location = (hips.x + 8, hips.y, 1.2)
    cam.rotation_euler = (math.radians(90), 0, math.radians(90))
    sc.render.filepath = f"{saida}_{i:02d}_{f}.png"
    bpy.ops.render.render(write_still=True)
print("OK", saida)