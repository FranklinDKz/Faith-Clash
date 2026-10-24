# Catálogo de animações (Mixamo)

Cada animação foi **analisada no Blender** (duração, deslocamento, altura do quadril, velocidade das mãos e pés, se fecha em loop)
e **renderizada em 6 poses** (`Ferramentas/analise/previews`). Os números ficam em `Ferramentas/analise/metricas/*.json`.
Depois todas passaram pelo teste automático dentro da Unity (`Faith Clash > Ferramentas > Testar Animações`,
resultado em `Ferramentas/analise/teste_animacoes.txt`): ele toca cada estado e mede a altura dos pés e das mãos em relação ao chão real.

As animações rodam na **velocidade original** do Mixamo. O deslocamento horizontal dos golpes vem do próprio clipe
(`MovimentoDaAnimacao`, com colisão); na locomoção quem move é o `PlayerController`.

## Locomoção
| Clipe (arquivo) | Uso no jogo |
|---|---|
| OffensiveIdle | Parado no modo Poder (postura de luta) |
| OrcIdle | Parado no modo Espada |
| HappyIdle | Descanso (depois de 12 s parado no modo Poder) |
| StandardRun | Correr (Poder) |
| Sprint | Correr rápido (Shift) |
| GreatSwordRun | Correr com a espada |
| RunningBackward / WalkingBackwards | Andar para trás com o alvo travado |
| IdleToSprint / StartWalking | Arranque do parado para a corrida |
| ChangeDirection | Reversão de direção em corrida |
| GreatSword180Turn | Virada de 180° (modo Espada) |
| SwordAndShield180Turn / SwordAndShieldTurn | Virada de 180° / 90° (modo Poder) |
| InjuredWalking | Andar ferido (catalogado) |
| Transition | Transição de postura (catalogado) |

## Movimento especial (C, Ctrl, Espaço)
| Clipe | Uso |
|---|---|
| CrouchedToSprinting | **Dash** (C) |
| RunningSlide | Deslize (Ctrl correndo) |
| Running (mergulho + rolamento) | Rolamento (Ctrl parado) |
| Jump / Jumping | Pulo / no ar |
| JumpingDown | Pouso pesado (queda alta) |
| FallingToRoll | Pouso com rolamento (queda muito alta ou em velocidade) |
| WallRun | Corrida na parede (sprint + pulo contra a parede) |

## Espada (G)
| Clipe | Uso |
|---|---|
| DrawAGreatSword2 / SheathAGreatSword2 | Sacar / guardar a katana |
| GreatSwordSlash (3 cortes) | Combo: Corte 1, 2 e 3 |
| SwordAndShieldSlash | 4º golpe (corte giratório) |
| GreatSwordJumpAttack | Golpe pesado (segurar o botão esquerdo) |
| GreatSwordSlideAttack | Ataque correndo (Shift + clique) |
| SwordAndShieldAttack | Ataque aéreo (catalogado) |
| StandingBlockIdle | Defesa (botão direito) |

## Poder: punhos, chutes e magias
| Clipe | Uso |
|---|---|
| FlyingKneePunchCombo (3 golpes) | Soco 1, 2 e 3 |
| GreatSwordKick | 4º golpe (chute) |
| Martelo 2 (chute giratório) | Golpe pesado do modo Poder |
| Standing2HMagicAttack03 | Gesto do Katon (Goukakyuu) |
| Standing2HMagicAttack04 | Gesto do Fuuton / Katon (Housenka) |
| Standing2HCastSpell01 | Gesto do Suiton (Suiryuudan) / Azul |
| SwordAndShieldCasting | Gesto do Doton / Suiton (Suirou) |
| SwordAndShieldPowerUp | Roxo (R), Reppushou, frasco de fúria |
| MagicHeal | Poção de vida e cristal de Fé |
| PickUpItem | Lanterna e outros itens |

## Morte (sorteada)
StandingReactDeathBackward, StandingReactDeathForward, FallingBackDeath, FallingForwardDeath, SwordAndShieldDeath, TwoHandedSwordDeath.

> Alguns arquivos vieram duplicados (versões `(1)` e a leva antiga com prefixo `Ch06_nonPBR@`). As duplicadas ficam só no catálogo; o jogo usa uma de cada.
