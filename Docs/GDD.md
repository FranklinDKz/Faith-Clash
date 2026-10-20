# Faith Clash - Game Design Document (resumo)

## Visao
Combate 3D em terceira pessoa, num mundo pos-guerra mundial (2026-2030) onde armas de fogo convivem com katanas, jutsus, artes marciais e poderes sobrenaturais. O jogador cria o proprio personagem e escreve a propria historia.

## Pilares
1. **Combate com peso e fluidez** - combos, esquiva, defesa, leitura do inimigo.
2. **Mistura de estilos** - corpo a corpo, lamina, tecnica/poder e arma de fogo no mesmo sistema.
3. **Identidade propria** - criacao de personagem e build; inspiracao em animes/jogos, sem copiar conteudo protegido.
4. **Offline primeiro, online depois** - arquitetura pronta para 1v1 e PvP.

## Modos
- **Treino** (primeiro): arena com bonecos, estatisticas de dano/DPS, reset rapido, depois selecao de estilo, bonecos que atacam e frame data.
- **Historia**: campanha com capitulos, dialogos e chefes.
- **Arcade**: sequencia de lutas, ranking local.
- **Livre**: mundo aberto, missoes, PvP futuro.

## Sistemas
| Sistema | Notas |
|---|---|
| Movimento | WASD, corrida, pulo, esquiva (feito) |
| Combate melee | combo leve x3, pesado, defesa (feito, prototipo) |
| Dano/Vida | `Health` + `IDamageable` (feito) |
| Estilos | Katana, Punhos, Jutsu, Arma de fogo, Poder |
| Recursos | Vida, Estamina, Energia (chakra/ki/energia amaldicoada) |
| Criacao de personagem | aparencia, estilo inicial, atributos |
| Inimigos/IA | bonecos -> NavMesh + estados |
| Online | Netcode for GameObjects + Unity Gameplay Services (futuro) |

## Roadmap
1. **M1 - Treino base** (agora): movimento, camera, combo, dummies, HUD.
2. **M2 - Treino v2**: estamina, defesa com perfect-block, bonecos com IA, animacoes reais (Mixamo/proprias), VFX.
3. **M3 - Estilos**: Katana, Punhos, um Jutsu a distancia, arma de fogo.
4. **M4 - Criacao de personagem + Menu principal**.
5. **M5 - Arcade**, depois **Historia**.
6. **M6 - Online 1v1** (Unity Multiplayer Services).
7. **M7 - Porte mobile**.

## Arte e tom
Paleta: azul-noite `#0a0c18`, dourado `#ffe08a`/`#d68a1c`, laranja de explosao `#ff5a2b`. Ruinas urbanas, luz de fim de tarde, fe como tema (cruz/escudo no logo).

## Nota legal
Referencias a Jujutsu Kaisen, Naruto, Dragon Ball Z, Naraka e Elden Ring sao apenas inspiracao de mecanica. Nomes, personagens, tecnicas e assets de terceiros nao devem entrar no jogo se for publicar/vender.
