# Para cada FBX do Mixamo: renderiza uma folha de poses, mede a animacao (duracao, deslocamento, altura,
# velocidade das maos/pes, se fecha em loop) e exporta so o esqueleto (arquivo leve).
# Uso: blender -b -P analisar_e_extrair.py -- <entrada.fbx> <nome_clipe> <pasta_saida_fbx> <pasta_previews> <pasta_metricas>
import bpy, sys, math, json, os
from mathutils import Vector

entrada, nome, pasta_fbx, pasta_prev, pasta_met = sys.argv[sys.argv.index("--") + 1:][:5]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=entrada)
sc = bpy.context.scene
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
act = arm.animation_data.action
f0, f1 = int(act.frame_range[0]), int(act.frame_range[1])
fps = sc.render.fps
P = "mixamorig9:"
pb = arm.pose.bones


def pos(osso):
    return arm.matrix_world @ pb[P + osso].head


# ---------------------------------------------------------------- metricas
partes = ["RightHand", "LeftHand", "RightFoot", "LeftFoot", "Hips", "Head"]
traj = {p: [] for p in partes}
for f in range(f0, f1 + 1):
    sc.frame_set(f)
    for p in partes:
        traj[p].append(pos(p).copy())
n = len(traj["Hips"])


def vel(p):
    t = traj[p]
    return [(t[i + 1] - t[i]).length * fps for i in range(len(t) - 1)]


def picos(v, lim):
    out = []
    for i in range(1, len(v) - 1):
        if v[i] >= lim and v[i] >= v[i - 1] and v[i] >= v[i + 1]:
            out.append(round(i / fps, 2))
    return out[:8]


vm = {p: vel(p) for p in partes}
hips = traj["Hips"]
desloc = hips[-1] - hips[0]
pes_z = [min(traj["RightFoot"][i].z, traj["LeftFoot"][i].z) for i in range(n)]
pose_ini = [pos(p) - hips[0] for p in ("RightHand", "LeftHand", "RightFoot", "LeftFoot")]
sc.frame_set(f1)
pose_fim = [pos(p) - hips[-1] for p in ("RightHand", "LeftHand", "RightFoot", "LeftFoot")]
dif_loop = sum((a - b).length for a, b in zip(pose_ini, pose_fim)) / 4.0
mx_maos = max(max(vm["RightHand"]), max(vm["LeftHand"]))
met = {
    "clipe": nome, "frames": n, "fps": fps, "duracao": round(n / fps, 2),
    "deslocamento_xy": round(math.hypot(desloc.x, desloc.y), 2), "deslocamento_z": round(desloc.z, 2),
    "hips_z_min": round(min(h.z for h in hips), 2), "hips_z_max": round(max(h.z for h in hips), 2),
    "cabeca_z_min": round(min(h.z for h in traj["Head"]), 2),
    "pes_z_min": round(min(pes_z), 3), "pes_z_max": round(max(pes_z), 3),
    "vel_max_maos": round(mx_maos, 1), "vel_max_pes": round(max(max(vm["RightFoot"]), max(vm["LeftFoot"])), 1),
    "picos_mao_direita": picos(vm["RightHand"], 0.6 * max(vm["RightHand"])),
    "picos_pe": picos(vm["RightFoot"] if max(vm["RightFoot"]) > max(vm["LeftFoot"]) else vm["LeftFoot"], 0.6 * max(max(vm["RightFoot"]), max(vm["LeftFoot"]))),
    "dif_loop": round(dif_loop, 3),
    "vel_media_xy": round(math.hypot(desloc.x, desloc.y) / max(n / fps, 0.01), 2),
}
os.makedirs(pasta_met, exist_ok=True)
json.dump(met, open(os.path.join(pasta_met, nome + ".json"), "w"), ensure_ascii=False)

# ---------------------------------------------------------------- folha de poses (6 quadros, frente e lateral)
os.makedirs(pasta_prev, exist_ok=True)
cd = bpy.data.cameras.new("c"); cd.type = "ORTHO"; cd.ortho_scale = 2.9
cam = bpy.data.objects.new("c", cd); sc.collection.objects.link(cam); sc.camera = cam
sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.light = "STUDIO"; sc.display.shading.color_type = "OBJECT"
sc.render.resolution_x, sc.render.resolution_y = 200, 260
sc.render.film_transparent = False
bpy.context.scene.world = bpy.data.worlds.new("w"); bpy.context.scene.world.color = (0.35, 0.4, 0.45)
# chao como referencia
bpy.ops.mesh.primitive_plane_add(size=6, location=(0, 0, 0))
chao = bpy.context.object
chao.name = "chao"
quadros = [f0 + int(i * (f1 - f0) / 5) for i in range(6)]
for qi, f in enumerate(quadros):
    sc.frame_set(f)
    centro = hips[min(max(f - f0, 0), n - 1)]
    cam.location = (centro.x + 7, centro.y - 7 * 0.0 - 0.0, 1.0)  # lateral (olhando para -X)
    cam.rotation_euler = (math.radians(90), 0, math.radians(90))
    cd.ortho_scale = 2.9
    sc.render.filepath = os.path.join(pasta_prev, f"{nome}_{qi}.png")
    bpy.ops.render.render(write_still=True)
bpy.data.objects.remove(chao, do_unlink=True)

# ---------------------------------------------------------------- exporta so o esqueleto
for o in list(bpy.data.objects):
    if o.type == "MESH":
        bpy.data.objects.remove(o, do_unlink=True)
act.name = nome
bpy.ops.object.select_all(action="DESELECT")
arm.select_set(True)
bpy.context.view_layer.objects.active = arm
os.makedirs(pasta_fbx, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=os.path.join(pasta_fbx, nome + ".fbx"), use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=1.0,
    apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", path_mode="STRIP",
)
print("OK", nome, met["duracao"])
