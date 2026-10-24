# Exporta as posições (mundo) dos ossos de uma animação para JSON; desenhar_esqueleto.py faz as imagens (vista lateral e frontal).
# Uso: blender -b -P esqueleto_dump.py -- <animacao.fbx> <saida.json> <quadro_ini> <quadro_fim> <n>
import bpy, sys, json
fbx, saida, f0, f1, n = sys.argv[sys.argv.index("--") + 1:][:5]
f0, f1, n = int(f0), int(f1), int(n)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
sc = bpy.context.scene
nomes = [b.name for b in arm.pose.bones if not any(x in b.name for x in ("Thumb", "Index", "Middle", "Ring", "Pinky", "HeadTop", "Toe_End"))]
out = {"bones": {b.name.split(":")[-1]: (b.parent.name.split(":")[-1] if b.parent else None) for b in arm.pose.bones if b.name in nomes}, "frames": []}
for i in range(n):
    f = f0 + int(round(i * (f1 - f0) / max(1, n - 1)))
    sc.frame_set(f)
    pos = {}
    for b in arm.pose.bones:
        if b.name in nomes:
            h = arm.matrix_world @ b.head
            pos[b.name.split(":")[-1]] = [round(h.x, 4), round(h.y, 4), round(h.z, 4)]
    out["frames"].append({"f": f, "p": pos})
json.dump(out, open(saida, "w"))
print("OK")
