# Catálogo de animações

Todas as animações são **gratuitas** e vêm de pacotes públicos (veja `CREDITOS_ASSETS.md`). Cada uma é cortada por quadros e recebe um nome de estado em
`PreparadorJogador.Clipes` (arquivo, quadro inicial/final, loop e quadro do impacto). A duração e o momento do impacto de cada golpe são gerados em
`Resources/InfoGolpes.txt`, então o dano e o som saem exatamente no quadro certo da animação.

Dois testes automáticos (menu Faith Clash > Ferramentas): **Testar Animações** mede pés e mãos contra o chão real de cada estado e **Testar Combate**
simula teclas e cliques, confere cadeias, impactos, modos, meditação, pulo e pouso, e tira fotos de cada golpe (`Ferramentas/analise/fotos`).

## Locomoção
| Estado | Origem |
|---|---|
| IdlePoder / IdleEspada / IdleCombate | Kevin Iglesias Idle01 / UAL1 Sword_Idle / Motifect muay_thai_guard_stance |
| Andar, Correr, Sprint | UAL1 Walk_Loop, Jog_Fwd_Loop, Sprint_Loop (velocidades naturais 0,97 / 5,36 / 8,25 m/s) |
| CorrerTras (alvo travado) | Kevin Iglesias Run01_Backward |
| Dash (C) | UAL2 Sword_Dash |
| Rolamento (Ctrl) | UAL1 Roll |
| Pulo | UAL1 Jump_Start, Jump_Loop (no ar), Jump_Land (pouso leve); UAL2 NinjaJump_Land (pouso pesado) |

## Combate (tecla 5): taijutsu
Jab, Direto (UAL1), Gancho (UAL2), Chute lateral (tkd roundhouse), Chute baixo (judo ankle sweep), Giro de costas (tkd spinning heel kick),
Chute frontal (muay thai teep), Cotovelada (muay thai elbow cut), Joelhada (muay thai knee), Chute machado (tkd axe kick), Chute lado (tkd side kick),
Chute costas (tkd back kick), Voadora e Voadora giratória (tkd jump kicks). Também no catálogo: Combinação muay thai, Chute muay.

## Espada (G)
Corte 1 e 2 (UAL2 Sword_Regular_A/B + recuperação), Corte 3 (Sword_Regular_C), Corte giratório (Motifect), Golpe pesado (Sword_Heavy_Combo),
Estocada correndo (UAL1 Sword_Attack), Corte para cima e diagonal, Contra-ataque, Sacar e Guardar (Motifect). Defesa: sword_block_high.

## Poder (1–4)
Gestos de Kevin Iglesias: Direct2H (Fogo), Omni (Vento), Direct1H R (Energia/Água), Call1H L (Terra), CastingDamage (Power-up). Com versões Load/Cast para segurar e soltar.

## Itens, meditação, reações
Consumir (UAL2), Pegar item (UAL1), Meditar (UAL1 Sitting_Enter → Sitting_Idle_Loop → Sitting_Exit), Dano (Hit_Chest/Hit_Head), Empurrado (Hit_Knockback), Morte (Death01).
