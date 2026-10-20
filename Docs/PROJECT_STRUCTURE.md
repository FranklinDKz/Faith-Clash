# Estrutura do projeto

Tudo do jogo fica em `Assets/_FaithClash/` (o `_` mantém a pasta no topo).

| Pasta | Conteúdo |
|---|---|
| `Scripts/Core` | Health, IDamageable e bases compartilhadas |
| `Scripts/Player` | Controle do jogador e câmera |
| `Scripts/Combat` | Combate corpo a corpo, estilos e habilidades |
| `Scripts/Training` | Modo Treino (manager, bonecos, HUD) |
| `Scripts/UI`, `Data`, `Utils` | Interface, dados/configurações e utilitários |
| `Scripts/Editor` | Ferramentas de editor (menu **Faith Clash**) |
| `ScriptableObjects` | Estilos de luta, personagens e habilidades |
| `Art`, `Animations`, `Audio` | Modelos, texturas, VFX, animações, música e efeitos sonoros |
| `Prefabs`, `Scenes`, `Materials`, `Settings` | Prefabs, cenas (`Scenes/Sandbox` para testes), materiais e configurações |

## Convenções
- Namespace `FaithClash.<Área>` (ex.: `FaithClash.Combat`).
- Dois assemblies: `FaithClash.Runtime` (`Scripts/`) e `FaithClash.Editor` (`Scripts/Editor`), para compilar mais rápido.
- Pastas vazias levam um arquivo `.gitkeep`.
- Assets pesados (fbx, imagens grandes, áudio) vão por Git LFS, via `.gitattributes`.
- Entrada de dados somente pelo Input System (o projeto não usa o sistema antigo).
- Comentários e documentação em português do Brasil; nomes de código em inglês.
- O menu **Faith Clash > Criar Cena de Treino** gera `Scenes/Training.unity`.
