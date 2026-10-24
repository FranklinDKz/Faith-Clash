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

## Controles (Modo Treino) — estilo Naraka: Bladepoint

| Ação | Teclado / Mouse | Controle |
|---|---|---|
| Mover | WASD | Analógico esquerdo |
| Câmera | Mouse | Analógico direito |
| Correr rápido (sprint) | Shift (segurar) | Clique no analógico esquerdo |
| Pular (segurar = pulo mais alto; solta cedo = pulo curto) | Espaço | A / X |
| **Dash para frente** | **C** | RB / R1 |
| **Rolamento** (esquiva; cancela o golpe) | Ctrl | B / Bolinha |
| **Golpe leve** (combo) / técnica leve no modo Poder | Clique esquerdo | X / Quadrado |
| **Golpe pesado** / técnica forte no modo Poder | **Segurar** o clique esquerdo | Y / Triângulo |
| **Defender** | Botão direito (segurar) | LT / L2 |
| Joelhada voadora (modo Combate) | Shift + clique esquerdo | — |
| **Travar no alvo** / trocar de alvo / soltar (segurar) | Q | Clique no analógico direito |
| Trocar de alvo | Roda do mouse | — |
| **Espada ⇄ Poder** | G | — |
| **Só combate** (sem espada nem poderes; de novo volta ao modo anterior) | **5** | — |
| **Meditar** (segurar: recupera Fé; qualquer movimento interrompe) | **E** | — |
| Estilo Fogo / Água / Terra / Ar (entra no modo Poder) | 1 / 2 / 3 / 4 | — |
| **Habilidade do estilo** (gesto + jutsu) | F | — |
| Roxo (gasta carga espiritual) | R | — |
| Itens | Z / X / B / V | — |
| Dica de combo / combate / fechar | Tab | — |
| Zerar estatísticas / Reiniciar | T / F5 | — |
| Mostrar / esconder o mouse | ' (aspas) | — |

As teclas seguem o esquema do Naraka: Bladepoint (clique = leve, segurar = pesado, botão direito = defesa, Shift = sprint, Espaço = pulo).
Teclas que o Naraka usa para outras coisas (itens, habilidades) ficam em Z X B V, F e R. Para mudar, edite `PlayerController`, `MeleeCombat` e `EstilosElementais`.

**Três modos de combate que não se misturam:**
- **Combate (tecla 5):** só taijutsu, sem espada nem poderes. Clique = cadeia **Jab → Direto → Gancho → Chute lateral**; **S** no 3º golpe vira Chute baixo, **A/D** no 4º vira Giro de costas.
  Segurar o clique dá um golpe forte que muda conforme o ponto da cadeia: **Uppercut** (sozinho), **Cotovelada** (após 1), **Joelhada** (após 2), **Chute machado** (após 3 ou mais).
  Correndo, o clique solta a Joelhada voadora e segurar solta o Chute giratório. Os golpes fortes têm chance de Faísca Negra (Kokusen). O golpe seguinte pode sair logo depois do impacto (cancelamento), e o Ctrl ou o C cancelam o golpe.
- **Espada (katana, tecla G):** só corta. Combo de 4 cortes e golpe pesado com salto.
- **Poder (teclas 1–4):** jutsus de Naruto e técnicas de Jujutsu Kaisen, **sem socos nem chutes**. Clique = técnica leve do estilo, segurar = técnica forte, F = habilidade do estilo, R = Roxo. Cada técnica tem o próprio gesto (animação) e som e gasta Fé.

**Meditar (E):** segure parado para recuperar Fé bem mais rápido (aura azul). Mover, pular, atacar ou levar dano interrompe.

Sem alvo travado há uma assistência leve de mira nos ataques. Com alvo travado (Q) o personagem encara o inimigo e cada golpe avança só o necessário para alcançá-lo.

Catálogos: [animações](Docs/CATALOGO_ANIMACOES.md) e [sons](Docs/CATALOGO_SONS.md).

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
