# Renderiza o retrato do protagonista (usado no HUD).
# Uso: blender -b Protagonista.blend -P retrato.py -- <saida.png>
import bpy, sys, math

saida = sys.argv[sys.argv.index("--") + 1]
sc = bpy.context.scene
arm = bpy.data.objects["Esqueleto"]
arm.animation_data.action = None
for pb in arm.pose.bones:
    pb.rotation_quaternion = (1, 0, 0, 0)
    pb.location = (0, 0, 0)

cam_d = bpy.data.cameras.new("retrato")
cam_d.lens = 85
cam = bpy.data.objects.new("retrato", cam_d)
sc.collection.objects.link(cam)
sc.camera = cam
ang = math.radians(-12)
dist = 1.0
alvo = (0, 0, 1.64)
cam.location = (alvo[0] + math.sin(ang) * dist, alvo[1] - math.cos(ang) * dist, alvo[2] + 0.02)
cam.rotation_euler = (math.radians(89), 0, ang)

sc.render.engine = "BLENDER_WORKBENCH"
sc.display.shading.light = "STUDIO"
sc.display.shading.color_type = "TEXTURE"
sc.display.shading.show_cavity = True
sc.render.film_transparent = True
sc.render.resolution_x = sc.render.resolution_y = 512
sc.render.filepath = saida
bpy.ops.render.render(write_still=True)
