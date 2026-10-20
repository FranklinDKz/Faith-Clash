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
| Correr | Shift | Clique no analógico esquerdo |
| Pular | Espaço | A / X |
| Esquiva | Ctrl | B / Bolinha |
| Golpe leve (combo x3) | Botão esquerdo | X / Quadrado |
| Golpe pesado | Botão direito | Y / Triângulo |
| Defender | Q | LT / L2 |
| Zerar estatísticas / Reiniciar | T / R | — |
| Soltar o mouse | Esc | — |

## Como rodar

1. Instale o **Unity Hub** e o editor **6000.6.6f1**.
2. Clone o repositório e abra a pasta pelo Unity Hub.
3. No menu, clique em **Faith Clash > Criar Cena de Treino** (gera `Assets/_FaithClash/Scenes/Training.unity`).
4. Aperte **Play**.

## Estrutura

```
Assets/_FaithClash/
  Scripts/{Core,Player,Combat,Training,UI,Data,Utils,Editor}
  Art  Animations  Audio  Prefabs  Scenes  Materials  ScriptableObjects  Settings
Branding/   logo e banners prontos para redes sociais, Steam e GitHub
Docs/       GDD e estrutura do projeto
```

Mais detalhes em [Docs/PROJECT_STRUCTURE.md](Docs/PROJECT_STRUCTURE.md).

## Roadmap

Veja o [GDD](Docs/GDD.md).

## Licença

Todos os direitos reservados até a definição de uma licença. Jogo original: nenhum asset de franquias de terceiros deve ser incluído no repositório.
