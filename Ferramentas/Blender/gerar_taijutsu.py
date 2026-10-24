# Gera golpes de taijutsu (jab, direto, gancho, uppercut, cotovelada, joelhada, chutes, giro) no esqueleto Mixamo.
# Cada golpe é definido por poses-chave (quadril, tronco, alvos de IK das mãos e dos pés); o Blender resolve os braços e as pernas
# com IK, o resultado é "assado" nos ossos e exportado como UM arquivo com todos os golpes em sequência
# (o Unity corta os clipes pelos quadros listados em PreparadorJogador.Recortes["Taijutsu"]).
#
# Uso: blender -b -P gerar_taijutsu.py -- <esqueleto_base.fbx> <saida.fbx>
# Sistema local do personagem: r = para a direita dele, f = para frente, u = para cima (metros, origem no chão sob o quadril).
import bpy, sys, math
from mathutils import Vector, Quaternion, Matrix

entrada, saida = sys.argv[sys.argv.index("--") + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=entrada)
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type == "MESH":
        bpy.data.objects.remove(o, do_unlink=True)
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
bpy.context.view_layer.objects.active = arm
P = "mixamorig9:"

# ----------------------------------------------------------------------------- pose base (parado)
sc.frame_set(2)
bpy.context.view_layer.update()
pb = arm.pose.bones
base_basis = {b.name: (b.location.copy(), b.rotation_quaternion.copy(), b.scale.copy()) for b in pb}
base_world = {b.name: arm.matrix_world @ b.matrix for b in pb}
arm.animation_data_clear()
for b in pb:
    b.location, b.rotation_quaternion, b.scale = base_basis[b.name]
bpy.context.view_layer.update()

hips0 = base_world[P + "Hips"].translation.copy()
O = Vector((hips0.x, hips0.y, 0.0))
FWD = Vector((0, -1, 0))
RGT = Vector((-1, 0, 0))
UP = Vector((0, 0, 1))
AX_LEAN = UP.cross(FWD)        # girar em torno deste eixo inclina o tronco para frente


def loc(v):
    """(r, f, u) -> mundo"""
    return O + RGT * v[0] + FWD * v[1] + UP * v[2]


def local_of(w):
    d = w - O
    return (d.dot(RGT), d.dot(FWD), d.z)


B = {k: local_of(base_world[P + n].translation) for k, n in
     {"LH": "LeftHand", "RH": "RightHand", "LF": "LeftFoot", "RF": "RightFoot"}.items()}
print("BASE", B)

# ----------------------------------------------------------------------------- alvos de IK
def empty(nome, tipo="PLAIN_AXES", tam=0.08):
    e = bpy.data.objects.new(nome, None)
    e.empty_display_type = tipo
    e.empty_display_size = tam
    sc.collection.objects.link(e)
    e.rotation_mode = "QUATERNION"
    return e

T = {k: empty("T_" + k) for k in ("LH", "RH", "LF", "RF")}
PL = {k: empty("P_" + k, "SPHERE", 0.05) for k in ("LH", "RH", "LF", "RF")}
FR = {k: empty("R_" + k, "ARROWS") for k in ("LF", "RF")}

IK_OSSO = {"LH": "LeftForeArm", "RH": "RightForeArm", "LF": "LeftLeg", "RF": "RightLeg"}
CON = {}
for k, n in IK_OSSO.items():
    c = pb[P + n].constraints.new("IK")
    c.target, c.pole_target = T[k], PL[k]
    c.chain_count = 2
    c.use_stretch = False
    c.influence = 0.0
    CON[k] = c
ROTCON = {}
for k, n in {"LF": "LeftFoot", "RF": "RightFoot"}.items():
    c = pb[P + n].constraints.new("COPY_ROTATION")
    c.target = FR[k]
    c.target_space = c.owner_space = "WORLD"
    c.influence = 1.0
    ROTCON[k] = c
FOOT_Q0 = {k: base_world[P + n].to_quaternion() for k, n in {"LF": "LeftFoot", "RF": "RightFoot"}.items()}

# calibra o ângulo do polo (para o cotovelo / joelho dobrar para o lado certo)
POLO_TESTE = {"LH": (-0.55, 0.2, 1.1), "RH": (0.55, 0.2, 1.1), "LF": (-0.25, 1.2, 0.7), "RF": (0.25, 1.2, 0.7)}
ALVO_TESTE = {"LH": (-0.2, 0.55, 1.4), "RH": (0.2, 0.55, 1.4), "LF": (-0.2, 0.4, 0.45), "RF": (0.2, 0.4, 0.45)}
JUNTA = {"LH": "LeftForeArm", "RH": "RightForeArm", "LF": "LeftLeg", "RF": "RightLeg"}
for k in IK_OSSO:
    T[k].location = loc(ALVO_TESTE[k])
    PL[k].location = loc(POLO_TESTE[k])
    CON[k].influence = 1.0
    melhor, ang_m = 1e9, 0
    for a in range(-180, 181, 10):
        CON[k].pole_angle = math.radians(a)
        bpy.context.view_layer.update()
        cot = arm.matrix_world @ pb[P + JUNTA[k]].head
        d = (cot - PL[k].location).length
        if d < melhor:
            melhor, ang_m = d, a
    CON[k].pole_angle = math.radians(ang_m)
    CON[k].influence = 0.0
    print("POLO", k, ang_m, round(melhor, 3))

# ----------------------------------------------------------------------------- definição dos golpes
GL, GR = (-0.20, 0.22, 1.40), (0.18, 0.18, 1.38)      # guarda (perto do queixo: o braço é curto, ~0,46 m)
POLO_E, POLO_D = (-0.55, 0.05, 1.05), (0.55, 0.05, 1.05)
KNEE_L, KNEE_R = (-0.30, 1.0, 0.7), (0.30, 1.0, 0.7)

# parâmetros e valores neutros
NEUTRO = dict(
    hd=(0, 0, 0), hyaw=0, hlean=0, syaw=0, slean=0, spin=0,
    LH=GL, LHw=0, LHp=POLO_E, RH=GR, RHw=0, RHp=POLO_D,
    LF=None, LFw=1, LFp=KNEE_L, LFyaw=0, LFpitch=0,
    RF=None, RFw=1, RFp=KNEE_R, RFyaw=0, RFpitch=0,
)
NEUTRO["LF"], NEUTRO["RF"] = B["LF"], B["RF"]


def K(f, modo="s", g=True, **kw):
    d = dict(LHw=1, RHw=1) if g else {}   # g=True: guarda levantada (os dois punhos com IK)
    d.update(kw)
    return (f, modo, d)


# cada golpe: (nome, quadro inicial, quadro final, chaves). Chaves usam quadros relativos ao início do bloco.
GOLPES = [
    ("Jab", 0, 17, [
        K(0, g=False), K(2, hyaw=-4),
        K(5, "o", LH=(-0.10, 0.80, 1.43), LHp=(-0.45, 0.35, 1.10), hd=(0, 0.14, -0.02), hyaw=-14, syaw=-8, hlean=4),
        K(8, LH=(-0.18, 0.55, 1.42), hd=(0, 0.10, -0.01), hyaw=-8, syaw=-4),
        K(12, hyaw=-2), K(17, LHw=0, RHw=0),
    ]),
    ("Direto", 20, 40, [
        K(0, g=False), K(3),
        K(5, hyaw=-14, syaw=-12, hd=(0, -0.04, -0.03), RH=(0.22, 0.24, 1.38)),
        K(8, "o", RH=(0.06, 0.84, 1.41), RHp=(0.55, 0.30, 1.10), LH=(-0.22, 0.30, 1.46), hd=(0, 0.20, -0.03), hyaw=30, syaw=22, hlean=5,
          RFyaw=25),
        K(12, RH=(0.10, 0.60, 1.40), hd=(0, 0.14, -0.02), hyaw=18, syaw=10, RFyaw=15),
        K(16, hyaw=4), K(20, LHw=0, RHw=0),
    ]),
    ("Gancho", 43, 64, [
        K(0, g=False), K(3),
        K(5, hyaw=-20, syaw=-16, LH=(-0.52, 0.22, 1.30), LHp=(-0.65, 0.0, 1.30), hd=(0, -0.03, -0.04)),
        K(8, "o", LH=(-0.10, 0.62, 1.40), LHp=(-0.55, 0.45, 1.65), hyaw=36, syaw=26, hd=(0, 0.14, -0.04), hlean=4, LFyaw=-10),
        K(12, LH=(-0.18, 0.50, 1.40), hyaw=18, syaw=10),
        K(16, hyaw=4), K(21, LHw=0, RHw=0),
    ]),
    ("Uppercut", 67, 91, [
        K(0, g=False), K(3),
        K(6, hd=(0, 0.04, -0.20), hlean=14, hyaw=-14, syaw=-12, RH=(0.16, 0.32, 0.98), RHp=(0.6, 0.1, 0.9)),
        K(9, "o", hd=(0, 0.16, 0.02), hlean=-6, hyaw=24, syaw=14, RH=(0.10, 0.62, 1.68), RHp=(0.6, 0.5, 1.35), RFyaw=25),
        K(13, hd=(0, 0.10, 0.0), hyaw=14, RH=(0.14, 0.45, 1.5)),
        K(18, hyaw=3), K(24, LHw=0, RHw=0),
    ]),
    ("Cotovelada", 94, 115, [
        K(0, g=False), K(3),
        K(6, hyaw=-22, syaw=-14, RH=(0.30, 0.10, 1.40), RHp=(0.80, -0.1, 1.30)),
        K(9, "o", RH=(0.00, 0.34, 1.46), RHp=(0.35, 1.10, 1.50), hyaw=34, syaw=22, hd=(0, 0.20, -0.04), hlean=6, RFyaw=22),
        K(13, RH=(0.10, 0.40, 1.42), RHp=(0.4, 0.8, 1.4), hyaw=22, hd=(0, 0.14, -0.03)),
        K(18, hyaw=4), K(21, LHw=0, RHw=0),
    ]),
    ("Joelhada", 118, 142, [
        K(0, g=False), K(3),
        K(6, hd=(0, 0.06, -0.04), LH=(-0.18, 0.40, 1.30), RH=(0.18, 0.40, 1.30)),
        K(9, "o", hd=(0, 0.14, 0.04), hlean=-12, LH=(-0.20, 0.45, 1.25), RH=(0.20, 0.45, 1.25),
          RF=(0.14, 0.48, 1.04), RFw=1, RFp=(0.15, 1.3, 1.2), RFpitch=-35),
        K(14, hd=(0, 0.10, 0.02), RF=(0.14, 0.40, 0.80), RFw=1, RFp=(0.15, 1.2, 1.0), RFpitch=-20),
        K(19, RF=B["RF"]), K(24, LHw=0, RHw=0),
    ]),
    ("ChuteLateral", 145, 174, [
        K(0, g=False), K(3),
        K(7, hd=(0, -0.02, 0), hyaw=-10, hlean=6, RF=(0.32, 0.26, 0.88), RFw=1, RFp=(0.9, 0.5, 0.95), RFyaw=15),
        K(11, "o", hd=(0, 0.06, -0.02), hyaw=42, syaw=14, hlean=-18, RF=(0.02, 0.88, 1.16), RFw=1, RFp=(0.7, 0.7, 1.40),
          RFyaw=55, LFyaw=55, RFpitch=10),
        K(16, hyaw=34, hlean=-8, RF=(0.12, 0.72, 0.95), RFw=1, RFp=(0.7, 0.8, 1.1), RFyaw=40, LFyaw=40),
        K(22, hyaw=8, RF=(0.17, 0.2, 0.40), RFw=1, RFyaw=0), K(29, LHw=0, RHw=0),
    ]),
    ("ChuteBaixo", 177, 203, [
        K(0, g=False), K(3),
        K(6, hyaw=-12, hlean=14, hd=(0, 0.0, -0.08), RF=(0.34, 0.14, 0.45), RFw=1, RFp=(0.9, 0.3, 0.6)),
        K(10, "o", hyaw=34, hlean=16, hd=(0, 0.10, -0.14), RF=(0.05, 0.88, 0.40), RFw=1, RFp=(0.6, 0.8, 0.8),
          RFyaw=45, LFyaw=40),
        K(15, hyaw=24, hlean=8, hd=(0, 0.08, -0.08), RF=(0.12, 0.60, 0.30), RFw=1, RFyaw=30, LFyaw=25),
        K(21, hyaw=6, RF=(0.17, 0.15, 0.20), RFw=1), K(26, LHw=0, RHw=0),
    ]),
    ("ChuteMachado", 206, 239, [
        K(0, g=False), K(3),
        K(10, hlean=-22, hd=(0, -0.05, 0.0), RF=(0.10, 0.48, 1.40), RFw=1, RFp=(0.9, 0.35, 1.30), RFpitch=-20,
          LH=(-0.22, 0.30, 1.50), RH=(0.22, 0.30, 1.50)),
        K(16, "o", hlean=16, hd=(0, 0.18, -0.10), RF=(0.06, 0.78, 0.34), RFw=1, RFp=(0.2, 1.3, 0.9), RFpitch=-5,
          LH=(-0.30, 0.20, 1.30), RH=(0.30, 0.20, 1.30)),
        K(22, hlean=8, hd=(0, 0.14, -0.06), RF=(0.17, 0.5, 0.15), RFw=1),
        K(28, RF=B["RF"]), K(33, LHw=0, RHw=0),
    ]),
    ("GiroCostas", 242, 269, [
        K(0, g=False), K(3),
        K(5, hyaw=-20, syaw=-10, spin=-20, LFyaw=-20, RFyaw=-20, hd=(0, 0, -0.06)),
        K(14, "s", hyaw=-250, syaw=-60, spin=-310, LFyaw=-250, RFyaw=-250, hd=(0, 0.05, -0.08),
          RH=(0.15, 0.55, 1.45), RHp=(0.6, 0.2, 1.35)),
        K(17, "o", hyaw=-345, syaw=-30, spin=-345, LFyaw=-345, RFyaw=-345, hd=(0, 0.14, -0.06),
          RH=(0.35, 0.75, 1.45), RHp=(0.6, 0.55, 1.4), LH=(-0.20, 0.30, 1.38)),
        K(21, hyaw=-360, syaw=-8, spin=-360, LFyaw=-360, RFyaw=-360, RH=(0.20, 0.55, 1.42)),
        K(26, hyaw=-360, spin=-360, LFyaw=-360, RFyaw=-360), K(27, LHw=0, RHw=0),
    ]),
]
ULTIMO = max(g[2] for g in GOLPES)


def ease(t, modo):
    t = max(0.0, min(1.0, t))
    if modo == "o":
        return 1 - (1 - t) ** 2.2
    if modo == "i":
        return t ** 2.0
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    if isinstance(a, tuple):
        return tuple(x + (y - x) * t for x, y in zip(a, b))
    return a + (b - a) * t


def estado_no_quadro(f):
    """Parâmetros do personagem no quadro absoluto f."""
    for nome, ini, fim, chaves in GOLPES:
        if ini <= f <= fim:
            rel = f - ini
            # estado em cada chave (valores acumulam: o que não é citado volta ao neutro)
            estados = []
            for (qf, modo, kw) in chaves:
                e = dict(NEUTRO)
                e.update(kw)
                estados.append((qf, modo, e))
            for i in range(len(estados) - 1):
                f0, _, e0 = estados[i]
                f1, m1, e1 = estados[i + 1]
                if f0 <= rel <= f1:
                    t = ease((rel - f0) / max(1, f1 - f0), m1)
                    return {k: lerp(e0[k], e1[k], t) for k in NEUTRO}
            return estados[-1][2]
    return dict(NEUTRO)


# ----------------------------------------------------------------------------- aplica a pose em cada quadro
def mw_set(bone, mw):
    bone.matrix = arm.matrix_world.inverted() @ mw


def rot_em_torno(mw, ponto, eixo, ang):
    R = Matrix.Rotation(math.radians(ang), 4, eixo)
    return Matrix.Translation(ponto) @ R @ Matrix.Translation(-ponto) @ mw


def rodar_local(v, spin):
    """gira (r,f,u) em torno do vertical (sentido anti-horário visto de cima = para a esquerda dele)"""
    a = math.radians(spin)
    x, y = v[0], v[1]
    # r aponta para a direita, f para frente: virar à esquerda (spin>0) leva f para -r
    r2 = x * math.cos(a) + y * math.sin(a) * (-1)
    f2 = x * math.sin(a) + y * math.cos(a)
    return (r2, f2, v[2])


def rot_ponto(p, eixo, ang):
    return Matrix.Translation(p) @ Matrix.Rotation(math.radians(ang), 4, eixo) @ Matrix.Translation(-p)


def aplicar(f):
    e = estado_no_quadro(f)
    # Tudo é calculado a partir da pose BASE (estática), nunca da pose do quadro anterior: nada se acumula entre os quadros.
    # C = transformação rígida acumulada do quadril para cima (quadril -> coluna -> pescoço -> cabeça).
    d = e["hd"]
    C = (Matrix.Translation(RGT * d[0] + FWD * d[1] + UP * d[2])
         @ rot_ponto(hips0, AX_LEAN, e["hlean"]) @ rot_ponto(hips0, UP, e["hyaw"]))
    mw_set(pb[P + "Hips"], C @ base_world[P + "Hips"])
    bpy.context.view_layer.update()
    # coluna: guinada e inclinação extras distribuídas; cabeça olha para a frente (compensa parte da torção)
    tot = (e["hyaw"] + e["syaw"] + 180) % 360 - 180
    cadeia = (("Spine", 0.3, e["syaw"], e["slean"]), ("Spine1", 0.35, e["syaw"], e["slean"]), ("Spine2", 0.35, e["syaw"], e["slean"]),
              ("Neck", 0.4, -tot * 0.7, 0.0), ("Head", 0.6, -tot * 0.7, 0.0))
    for nome, frac, yaw, lean in cadeia:
        ponto = (C @ base_world[P + nome]).translation
        C = rot_ponto(ponto, UP, yaw * frac) @ C
        if lean:
            C = rot_ponto(ponto, AX_LEAN, lean * frac) @ C
        mw_set(pb[P + nome], C @ base_world[P + nome])
        bpy.context.view_layer.update()
    for nome in ("Hips", "Spine", "Spine1", "Spine2", "Neck", "Head"):
        b = pb[P + nome]
        b.keyframe_insert("location", frame=f)
        b.keyframe_insert("rotation_quaternion", frame=f)
    # alvos de IK
    spin = e["spin"]
    # as mãos usam o referencial do TRONCO (projeção do peito no chão, girando com o quadril e a coluna):
    # o soco acompanha a torção e o avanço do corpo, e o braço (curto) sempre alcança o alvo
    h = arm.matrix_world @ pb[P + "Spine2"].head
    co = Vector((h.x, h.y, 0.0))
    giro = e["hyaw"] + e["syaw"]
    for k in ("LH", "RH"):
        r, f_, u = rodar_local(e[k], giro)
        T[k].location = co + RGT * r + FWD * f_ + UP * u
        r, f_, u = rodar_local(e[k + "p"], giro)
        PL[k].location = co + RGT * r + FWD * f_ + UP * u
        CON[k].influence = e[k + "w"]
    for k in ("LF", "RF"):
        T[k].location = loc(e[k])
        PL[k].location = loc(rodar_local(e[k + "p"], spin))
        CON[k].influence = e[k + "w"]
        q = (Quaternion(UP, math.radians(e[k + "yaw"])) @ Quaternion((1, 0, 0), math.radians(e[k + "pitch"])) @ FOOT_Q0[k])
        FR[k].rotation_quaternion = q
    for k in ("LH", "RH", "LF", "RF"):
        T[k].keyframe_insert("location", frame=f)
        PL[k].keyframe_insert("location", frame=f)
        CON[k].keyframe_insert("influence", frame=f)
    for k in ("LF", "RF"):
        FR[k].keyframe_insert("rotation_quaternion", frame=f)
    bpy.context.view_layer.update()
    # diagnóstico: quanto o IK errou o alvo (m)
    for k, osso in (("LH", "LeftHand"), ("RH", "RightHand"), ("LF", "LeftFoot"), ("RF", "RightFoot")):
        if e[k + "w"] > 0.95:
            err = (arm.matrix_world @ pb[P + osso].head - T[k].location).length
            if err > 0.04:
                ombro = arm.matrix_world @ pb[P + ("LeftArm" if k == "LH" else "RightArm")].head if k in ("LH", "RH") else None
                dist = round((ombro - T[k].location).length, 3) if ombro else -1
                print("ERRO_IK", f, k, round(err, 3), "dist_alvo_ombro", dist)


for f in range(0, ULTIMO + 1):
    sc.frame_set(f)
    aplicar(f)

print("CHAVES OK", ULTIMO)

# ----------------------------------------------------------------------------- assa (visual keying) e exporta
sc.frame_start, sc.frame_end = 0, ULTIMO
bpy.ops.object.mode_set(mode="POSE")
bpy.ops.pose.select_all(action="SELECT")
bpy.ops.nla.bake(frame_start=0, frame_end=ULTIMO, step=1, only_selected=False, visual_keying=True,
                 clear_constraints=True, use_current_action=True, bake_types={"POSE"})
bpy.ops.object.mode_set(mode="OBJECT")
arm.animation_data.action.name = "Taijutsu"
for e in list(T.values()) + list(PL.values()) + list(FR.values()):
    bpy.data.objects.remove(e, do_unlink=True)

if "--so-previa" not in sys.argv:
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
