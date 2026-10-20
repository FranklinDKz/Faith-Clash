<p align="center">
  <img src="Branding/logo_transparent.png" width="520" alt="Logo do Faith Clash">
</p>

<h1 align="center">Faith Clash</h1>

<p align="center">
  Jogo de combate 3D em terceira pessoa, num mundo pós-guerra mundial (2026–2030).<br>
  Katanas, jutsus, estilos de luta, armas de fogo e poderes — com personagem criado pelo jogador.
</p>

![Banner](Branding/banners/readme_banner_1500x500.png)

> **Status:** em desenvolvimento inicial. Modo Treino jogável (protótipo com formas primitivas).

## Sobre

Faith Clash é um jogo de combate para **PC (offline)**, feito na **Unity 6 (6000.6.6f1)** com URP. No futuro terá **online (1v1 e modo livre com PvP)** e versão **mobile**.

Inspirações: Jujutsu Kaisen, Naruto, Dragon Ball Z, Naraka: Bladepoint, Elden Ring e MMORPGs de aventura.

## Modos de jogo

| Modo | Descrição | Estado |
|---|---|---|
| Treino | Praticar combos, esquiva e defesa contra bonecos, com dano total e DPS | Protótipo |
| História | Campanha com personagem próprio | Planejado |
| Arcade | Lutas em sequência com pontuação | Planejado |
| Livre | Mundo aberto, PvP no futuro | Planejado |
| Online | Batalhas 1v1 e PvP livre | Futuro |

## Controles (Modo Treino)

| Ação | Teclado / Mouse | Controle |
|---|---|---|
| Mover | WASD | Analógico esquerdo |
| Câmera | Mouse | Analógico direito |
| Correr rápido (sprint) | Shift | Clique no analógico esquerdo |
| Pular / corrida na parede (sprint + pulo contra a parede) | Espaço | A / X |
| **Dash para frente** (estilo Naraka) | **C** | RB / R1 |
| Deslize / esquiva | Ctrl | B / Bolinha |
| Golpe leve (combo x4) | Botão esquerdo | X / Quadrado |
| Golpe pesado | Botão direito | Y / Triângulo |
| Travar no alvo / **trocar de alvo** / soltar (segurar) | **Q** | Clique no analógico direito |
| Trocar de alvo | Roda do mouse | — |
| Defender | F | LT / L2 |
| **Alternar Espada ⇄ Poder** | **G** | — |
| Estilo Fogo / Água / Terra / Ar (entra no modo Poder) | 1 / 2 / 3 / 4 | — |
| Roxo (gasta carga espiritual) | R | — |
| Itens | Z / X / B / V | — |
| Dica de combo / combate / fechar | Tab | — |
| Zerar estatísticas / Reiniciar | T / F5 | — |
| Mostrar / esconder o mouse | ' (aspas) | — |

**Dois modos de combate que não se misturam:**
- **Espada (katana):** só corta. Combo de 4 cortes e golpe pesado com salto.
- **Poder:** punhos e chutes (estilo Naruto) + jutsus (Katon, Suiton, Doton, Fuuton) no 3º e 4º golpe, e técnicas amaldiçoadas (Azul, Roxo, Faísca Negra) inspiradas em Jujutsu Kaisen.

Sem alvo travado há uma assistência leve de mira nos ataques. Com alvo travado (Q) o personagem encara o inimigo e avança até ele.

## Como rodar

1. Instale o **Unity Hub** e o editor **6000.6.6f1**.
2. Clone o repositório e abra a pasta pelo Unity Hub.
3. No menu, clique em **Faith Clash > Preparar Jogador** e depois em **Faith Clash > Criar Cena de Treino** (gera `Assets/_FaithClash/Scenes/Treino.unity`).
4. Aperte **Play**.

## Estrutura

```
Assets/_FaithClash/
  Scripts/{Core,Player,Combat,Training,UI,Data,Utils,Editor}
  Art  Animations  Audio  Prefabs  Scenes  Materials  ScriptableObjects  Settings
Branding/   logo e banners prontos para redes sociais, Steam e GitHub
Docs/       GDD e estrutura do projeto
Ferramentas/Blender/   script que gera o protagonista (malha, rig e animações)
```

Mais detalhes em [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md).

## Roadmap

Veja o [GDD](Docs/GDD.md).

## Licença

Todos os direitos reservados até a definição de uma licença. Jogo original: nenhum asset de franquias de terceiros deve ser incluído no repositório.
