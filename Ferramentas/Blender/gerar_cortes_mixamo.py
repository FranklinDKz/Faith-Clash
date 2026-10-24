# Gera Anim/Mixamo/cortes.csv a partir da análise de picos de velocidade (picos_mx.json): para cada golpe, onde ele começa, acaba e
# em quais quadros ele acerta. O PreparadorJogador lê esse CSV. Uso: python gerar_cortes_mixamo.py <picos_mx.json> <saida.csv>
import json, sys
d = json.load(open(sys.argv[1]))
# tipo: M = só mãos acertam, P = só pés, A = qualquer; "multi" = vários impactos no mesmo clipe
ATAQUES = {
 # socos / cotovelos / joelhos (taijutsu, estilo boxe e muay thai)
 "Lead_Jab":("M",0),"Jab_Cross":("M","multi"),"Body_Jab_Cross":("M","multi"),"Cross_Punch":("M",0),"Hook_Punch":("M",0),"Right_Hook":("M",0),"Hook":("M",0),
 "Uppercut":("M",0),"Surprise_Uppercut":("A","multi"),"Elbow_Uppercut_Combo":("M","multi"),"Uppercut_Jab":("M","multi"),"Quad_Punch":("M","multi"),
 "Punch_Combo":("M","multi"),"Combo_Punch":("M","multi"),"Jab_To_Elbow_Punch":("M","multi"),"Punch_To_Elbow_Combo":("M","multi"),
 "Illegal_Elbow_Punch":("M",0),"Elbow_Punching":("M",0),"Knee_Jab":("A","multi"),"Knee_Jabs_To_Uppercut":("A","multi"),"Illegal_Knee":("A",0),
 "Illegal_Headbutt":("A",0),"Standing_Melee_Punch":("M",0),
 # chutes (taekwondo, capoeira, anime)
 "Mma_Kick":("P",0),"Roundhouse_Kick":("P",0),"Side_Kick":("P",0),"Hurricane_Kick":("P","multi"),"Spin_Flip_Kick":("P",0),"Drop_Kick":("P",0),
 "Flying_Kick":("P",0),"Flip_Kick":("P",0),"Inside_Crescent_Kick":("P",0),"Scissor_Kick":("P",0),"Knee_Kick_Lead":("A",0),"Standing_Melee_Kick":("P",0),
 "Standing_Melee_Attack_Kick_Ver_1":("P",0),"Standing_Melee_Attack_Kick_Ver_2":("P",0),"Leg_Sweep":("P",0),"Martelo_2":("P",0),"Martelo_3":("P",0),
 "Meia_Lua_De_Compasso":("P",0),"Meia_Lua_De_Compasso_Back":("P",0),"Armada":("P",0),"Chapa_Giratoria":("P","multi"),"Chapa_Giratoria_2":("P",0),
 "Queshada_1":("P",0),"Chapa_2":("P",0),"Rasteira_2":("P",0),"Martelo_Do_Chau":("P",0),"Flying_Bicycle_Kick":("A",0),
 "Flying_Knee_Punch_Combo":("A","multi"),"Back_Flip_To_Uppercut":("A",0),"Inverted_Double_Kick_To_Kip_Up":("P",0),
 "Standing_Melee_Combo_Attack_Ver_1":("A","multi"),"Standing_Melee_Combo_Attack_Ver_2":("A","multi"),"Standing_Melee_Combo_Attack_Ver_3":("A","multi"),
 "Standing_Melee_Run_Jump_Attack":("A",0),"Standing_Melee_Attack_360_Low":("A",0),"Jump_Attack":("A",0),"Mutant_Jump_Attack":("A",0),
 # espada (Demon Slayer / Naraka)
 "Two_Hand_Sword_Combo":("M","multi"),"One_Hand_Sword_Combo":("M","multi"),"Great_Sword_High_Spin_Attack":("A",0),"Great_Sword_Jump_Attack":("A",0),
 "Stable_Sword_Outward_Slash":("M",0),"Stable_Sword_Inward_Slash":("M",0),"Dual_Weapon_Combo":("M","multi"),"Sword_Fight_One":("M","multi"),"Charge":("M",0),
 # poderes (gestos de jutsu / Dragon Ball / Jujutsu Kaisen)
 "Standing_2H_Magic_Attack_01":("M",0),"Standing_2H_Magic_Attack_02":("M",0),"Standing_2H_Magic_Attack_05":("M",0),
 "Standing_1H_Magic_Attack_01":("M",0),"Standing_1H_Magic_Attack_02":("M",0),"Standing_1H_Magic_Attack_03":("M",0),
 "Standing_2H_Magic_Area_Attack_01":("M",0),"Standing_2H_Magic_Area_Attack_02":("M",0),"Standing_1H_Cast_Spell_01":("M",0),
 "Two_Hand_Spell_Casting":("M",0),"Wide_Arm_Spell_Casting":("M",0),"Magic_Spell_Casting":("M",0),"Strong_Gesture":("M",0),
}
LOOPS = {"Sprint_Forward","Sprint_Left","Sprint_Right","Sprint_Backward","Flying","Floating","Ninja_Idle","Fighting_Idle","Standing_Block_Idle"}
linhas = []
for nome, o in sorted(d.items()):
    n = nome[:-4]
    if n in ATAQUES:
        kind, multi = ATAQUES[n]
        lim = {"M": ("MD", "ME"), "P": ("PD", "PE"), "A": ("MD", "ME", "PD", "PE")}[kind]
        pk = sorted({(p, v) for k in lim for p, v in o["picos"][k]})
        if not pk:
            pk = sorted({(p, v) for k in o["picos"] for p, v in o["picos"][k]})
        if not pk:
            continue
        forte = max(pk, key=lambda x: x[1])
        if multi:
            lim_v = max(v for _, v in pk) * 0.55
            hits = []
            for p, v in pk:
                if v >= lim_v and (not hits or p - hits[-1] > 7):
                    hits.append(p)
        else:
            hits = [forte[0]]
        ini = max(0, hits[0] - 14); fim = min(o["frames"] - 1, hits[-1] + 22)
        linhas.append(f"Mx_{n};Mixamo/{nome};{ini};{fim};0;{','.join(str(h) for h in hits)};1")
    else:
        loop = 1 if n in LOOPS else 0
        mov = 1 if o["desloc"] > 0.6 else 0
        linhas.append(f"Mx_{n};Mixamo/{nome};0;-1;{loop};;{mov}")
open(sys.argv[2], "w", encoding="utf-8").write("\n".join(linhas) + "\n")
print(len(linhas), "linhas")
