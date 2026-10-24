# Catálogo de animações (Mixamo)

Cada animação foi **analisada no Blender** (duração, deslocamento, altura do quadril, velocidade das mãos e pés, se fecha em loop)
e **renderizada em 6 poses** (`Ferramentas/analise/previews`). Os números ficam em `Ferramentas/analise/metricas/*.json`.
Depois todas passaram pelo teste automático dentro da Unity (`Faith Clash > Ferramentas > Testar Animações`,
resultado em `Ferramentas/analise/teste_animacoes.txt`): ele toca cada estado e mede a altura dos pés e das mãos em relação ao chão real.

Um segundo teste automático (`Faith Clash > Ferramentas > Testar Combate`, resultado em `Ferramentas/analise/teste_combate.txt` e fotos em `Ferramentas/analise/fotos`)
injeta teclas e cliques e confere: nada toca sozinho, cadeias e ramos de combo, impacto no quadro certo da animação, modos 5/G/1-4, meditação (E), altura do pulo e tipos de pouso.

As animações rodam na **velocidade original** do Mixamo. O deslocamento horizontal dos golpes vem do próprio clipe
(`MovimentoDaAnimacao`, com colisão); na locomoção quem move é o `PlayerController`.

## Locomoção
| Clipe (arquivo) | Uso no jogo |
|---|---|
| OffensiveIdle | Parado no modo Poder (postura de luta) |
| OrcIdle | Parado no modo Espada |
| HappyIdle | Catalogado (nada toca sozinho: parado fica sempre no idle do modo) |
| StandardRun | Correr (Poder) |
| Sprint | Correr rápido (Shift) |
| GreatSwordRun | Correr com a espada |
| RunningBackward / WalkingBackwards | Andar para trás com o alvo travado |
| IdleToSprint / StartWalking / ChangeDirection / Turn 180° / Turn 90° | **Removidos do jogo** (tocavam sozinhos e dessincronizavam); ficam só catalogados |
| InjuredWalking | Andar ferido (catalogado) |
| Transition | Transição de postura (catalogado) |

## Movimento especial (C, Ctrl, Espaço)
| Clipe | Uso |
|---|---|
| CrouchedToSprinting | **Dash** (C) |
| Running (mergulho + rolamento) | Rolamento (Ctrl) |
| Jump (cortado em 5 partes) | **Decolagem**, **subida**, **topo**, **queda** (blend tree pela velocidade vertical) e **pouso leve** |
| JumpingDown | Pouso pesado (queda alta; fica preso 0,7 s) |
| FallingToRoll | Pouso com rolamento (queda muito alta ou em velocidade) |

Removidos do jogo (catalogados): WallRun e RunningSlide (pareciam escalada/deslize quebrado), GreatSwordSlideAttack e SwordAndShieldAttack (ataque deslizante / aéreo).

## Espada (G)
| Clipe | Uso |
|---|---|
| DrawAGreatSword2 / SheathAGreatSword2 | Sacar / guardar a katana |
| GreatSwordSlash (3 cortes) | Combo: Corte 1, 2 e 3 |
| SwordAndShieldSlash | 4º golpe (corte giratório) |
| GreatSwordJumpAttack | Golpe pesado (segurar o botão esquerdo) |
| StandingBlockIdle | Defesa (botão direito) |

## Combate: taijutsu (tecla 5)
Os golpes **Jab, Direto, Gancho, Uppercut, Cotovelada, Joelhada, Chute lateral, Chute baixo, Chute machado e Giro de costas** foram criados no Blender
(`Ferramentas/Blender/gerar_taijutsu.py`): poses-chave do quadril, tronco e alvos de IK das mãos e dos pés, resolvidas no esqueleto do Mixamo e assadas num único arquivo (`Taijutsu.fbx`).
Os quadros de cada golpe estão em `PreparadorJogador.Recortes["Taijutsu"]`. Conferência visual: `Ferramentas/Blender/ver_golpe.sh`.

| Clipe | Uso |
|---|---|
| Jab, Direto, Gancho, Chute lateral | Cadeia leve (clique) |
| Chute baixo | Ramo: S no 3º golpe |
| Giro de costas | Ramo: A/D no 4º golpe |
| Uppercut, Cotovelada, Joelhada, Chute machado | Golpes fortes (segurar o clique), conforme o ponto da cadeia |
| FlyingKneePunchCombo (golpe 3, joelhada voadora) | Correndo + clique |
| Martelo 2 (chute giratório) | Correndo + segurar o clique |
| FlyingKneePunchCombo (socos 1 e 2), GreatSwordKick | Catalogados (reserva) |

## Poder: magias (teclas 1–4, só gestos, sem golpes de luta)
| Clipe | Uso |
|---|---|
| Standing2HMagicAttack03 | Gesto do Katon (Goukakyuu) |
| Standing2HMagicAttack04 | Gesto do Fuuton (Daitoppa) / Katon (Housenka) |
| Standing2HCastSpell01 | Gesto do Suiton (Suiryuudan) / Azul |
| SwordAndShieldCasting | Gesto do Doton / Suiton (Suirou) |
| SwordAndShieldPowerUp | Roxo (R), Reppushou, frasco de fúria |
| MagicHeal | Poção de vida, cristal de Fé e **meditação (E)** |
| PickUpItem | Lanterna e outros itens |

## Morte (sorteada)
StandingReactDeathBackward, StandingReactDeathForward, FallingBackDeath, FallingForwardDeath, SwordAndShieldDeath, TwoHandedSwordDeath.

> Alguns arquivos vieram duplicados (versões `(1)` e a leva antiga com prefixo `Ch06_nonPBR@`). As duplicadas ficam só no catálogo; o jogo usa uma de cada.
