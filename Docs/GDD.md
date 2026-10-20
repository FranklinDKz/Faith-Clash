# Faith Clash — Documento de Design do Jogo (resumo)

## Visão
Combate 3D em terceira pessoa, num mundo pós-guerra mundial (2026–2030) onde armas de fogo convivem com katanas, jutsus, artes marciais e poderes sobrenaturais. O jogador cria o próprio personagem e escreve a própria história.

## Pilares
1. **Combate com peso e fluidez** — combos, esquiva, defesa e leitura do inimigo.
2. **Mistura de estilos** — corpo a corpo, lâmina, técnica/poder e arma de fogo no mesmo sistema.
3. **Identidade própria** — criação de personagem e builds; inspiração em animes e jogos, sem copiar conteúdo protegido.
4. **Offline primeiro, online depois** — arquitetura pronta para 1v1 e PvP.

## Modos
- **Treino** (primeiro): arena com bonecos, estatísticas de dano/DPS e reinício rápido. Depois: seleção de estilo, bonecos que atacam e dados de frames.
- **História**: campanha com capítulos, diálogos e chefes.
- **Arcade**: sequência de lutas e ranking local.
- **Livre**: mundo aberto, missões e PvP no futuro.

## Sistemas
| Sistema | Notas |
|---|---|
| Movimento | WASD, corrida, pulo e esquiva (feito) |
| Combate corpo a corpo | combo leve x3, golpe pesado e defesa (feito, protótipo) |
| Dano/Vida | `Health` + `IDamageable` (feito) |
| Estilos | Katana, Punhos, Jutsu, Arma de fogo, Poder |
| Recursos | Vida, Estamina e Energia (chakra/ki/energia amaldiçoada) |
| Criação de personagem | aparência, estilo inicial e atributos |
| Inimigos/IA | bonecos → NavMesh + máquina de estados |
| Online | Netcode for GameObjects + Unity Gameplay Services (futuro) |

## Roadmap
1. **M1 — Treino base** (agora): movimento, câmera, combo, bonecos e HUD.
2. **M2 — Treino v2**: estamina, defesa com timing perfeito, bonecos com IA, animações reais e VFX.
3. **M3 — Estilos**: Katana, Punhos, um jutsu à distância e arma de fogo.
4. **M4 — Criação de personagem e menu principal**.
5. **M5 — Arcade** e depois **História**.
6. **M6 — Online 1v1** (Unity Multiplayer Services).
7. **M7 — Versão mobile**.

## Arte e tom
Paleta: dourado, roxo e preto, como na logo. Ruínas urbanas, luz de fim de tarde e a fé como tema (luz contra trevas).

## Nota legal
Jujutsu Kaisen, Naruto, Dragon Ball Z, Naraka e Elden Ring são apenas inspiração de mecânica. Nomes, personagens, técnicas e assets de terceiros não devem entrar no jogo caso ele seja publicado ou vendido.
