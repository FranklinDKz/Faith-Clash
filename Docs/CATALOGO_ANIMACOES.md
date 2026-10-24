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

## Mixamo (150 animações, prefixo `Mx_`)
Baixadas pelo Mixamo (FBX, sem malha, 30 fps) em `Anim/Mixamo`. Os quadros de início, fim e impacto de cada golpe vêm da análise de velocidade das mãos e dos pés
(`Ferramentas/Blender/gerar_cortes_mixamo.py` → `Anim/cortes_mixamo.csv`); golpes de vários acertos (combos) têm um impacto por acerto.

- **Estilos de luta do modo Combate** (direção apertada no primeiro golpe): neutro = boxe; **W** = chutes (Mma Kick → Side Kick → Roundhouse → Spin Flip Kick);
  **A/D** = capoeira (Martelo → Meia Lua de Compasso → Armada → Hurricane Kick); **S** = muay thai de perto (cotovelo → joelho → Knee Kick → Elbow Uppercut Combo).
- **Golpes fortes** (segurar): Surprise Uppercut, Knee Jabs To Uppercut, Melee Combo Ver. 2; com 8+ acertos seguidos, Flying Knee Punch Combo. Correndo: Flying Kick e Run Jump Attack.
- **Espada:** Two/One Hand Sword Combo, Dual Weapon Combo, High Spin Attack, Great Sword Jump Attack.
- **Poder:** gestos fortes e habilidade do estilo (Magic Attack 05, Area Attack 01/02, Wide Arm / Magic Spell Casting, Strong Gesture); também no catálogo: Standing Yell (carregar poder), Great Sword Power Up, Flying, Floating.
- **Esquiva (Ctrl):** para o lado ou para trás faz Standing Dodge Left/Right/Backward; para frente ou parado, rolamento.
- Também importadas (catálogo): acrobacias (Stylish Flip, Run To Flip, Front Flip, Backflip, Big Jump, Corkscrew Evade), corridas (Sprint Forward/Left/Right/Backward, Idle To Sprint, Sprint Turn),
  reações (Hit Reaction, Big/Medium/Light Hit To Head, Standing React Small/Large), mortes (Standing React Death x4, Falling Back Death, Dying), bloqueios (Standing Block Start/Idle/End, Center Block).

## Efeitos visuais prontos
`CatalogoVFX` (componente do jogador, preenchido pelo Preparar Jogador) liga 39 efeitos dos pacotes grátis da Asset Store às ações: impactos, jutsus (fogo, água, terra, vento, Azul, Roxo, Faísca Negra), poeira do dash e do pouso, aura do modo Poder, meditação.
