# Estrutura do projeto

Tudo do jogo fica em `Assets/_FaithClash/` (o `_` mantem a pasta no topo).

| Pasta | Conteudo |
|---|---|
| `Scripts/Core` | Health, IDamageable, bases compartilhadas |
| `Scripts/Player` | Controle do jogador e camera |
| `Scripts/Combat` | Combate melee, estilos, habilidades |
| `Scripts/Training` | Modo Treino (manager, dummies, HUD) |
| `Scripts/UI` `Data` `Utils` | Interface, dados/configs, utilitarios |
| `Scripts/Editor` | Ferramentas de editor (menu **Faith Clash**) |
| `ScriptableObjects` | Estilos de luta, personagens, habilidades |
| `Art` `Animations` `Audio` | Modelos, texturas, VFX, animacoes, musica/SFX |
| `Prefabs` `Scenes` `Materials` `Settings` | Prefabs, cenas (`Scenes/Sandbox` p/ testes), materiais, configs |

## Convencoes
- Namespace `FaithClash.<Area>` (ex.: `FaithClash.Combat`).
- Dois assemblies: `FaithClash.Runtime` (`Scripts/`) e `FaithClash.Editor` (`Scripts/Editor`). Compilacao incremental mais rapida.
- Pastas vazias levam `.gitkeep`.
- Assets pesados (fbx, png grandes, audio) ja vao por Git LFS via `.gitattributes`.
- Entrada de dados so pelo Input System (o projeto esta com Input System, nao o antigo).
- Menu **Faith Clash > Criar Cena de Treino** gera `Scenes/Training.unity`.
