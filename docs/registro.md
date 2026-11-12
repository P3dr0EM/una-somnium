# Somnium: registro de desenvolvimento

O que foi feito, em ordem. As decisões e os motivos estão em `decisoes.md`.

## 2026-11-11

### Ambiente
- Avaliação do projeto: Unity 6000.6.7f1 + URP + Cardboard; cenas `persistencia`, `area_externa` e `predio`; 6 modelos no Blender.
- Blender 5.2 testado em modo headless: geração de modelo por script, exportação em FBX e render de prévia (porta da referência) em `D:\Projetos\Somnium\Claude\teste-porta`.
- Unity 6000.6.7f1 instalado (módulo Android).
- Caches do Unity e do Gradle movidos para `D:\Unity\Cache`. O Gradle antigo (5,5 GB) foi movido do C: e o C: passou de 4 para 8,3 GB livres.
- Primeira importação do projeto feita em batch (sem erros de compilação). Log em `D:\Projetos\Somnium\Claude\logs`.

### Git
- Sincronizado com o commit do Caio `2c7722d` (portal WIP + reorganização em `Mechanics/` e `extras/`).
- Branch **`feature/sistemas-base`** criada a partir da `main` atualizada.
- `.gitignore` na raiz para backups do Blender; `models/pote_de_racao.blend1` tirado do repositório.

### Código (branch `feature/sistemas-base`, ainda sem commit)
- `PlayerInputLogic`:
  - nome da classe corrigido (era `MouseLook`) e `using` quebrado removido;
  - mantida a herança de `PortalTraveller` (Caio);
  - reescrito para PC + VR;
  - `Teleport` corrigido para funcionar com o CharacterController.
- `InputSystem_Actions`: botões reorganizados e ações novas `Inventory` e `Combine`. O backup do original está em `D:\Projetos\Somnium\Claude`.
- `Interactable` (Caio): adicionados `Prompt` e `CanInteract`, sem quebrar o que existia. `Itr_Door` mostra "Abrir" / "Fechar".
- Sistemas novos: GameInput, GameState/WorldState, HUD, mira (PlayerInteractor), ItemLock, ItemData/Inventory/ItemPickup/InventoryUI, inspeção 3D com InspectHotspot. Ver `sistemas-base.md`.
- Menu **Somnium** no editor: cria os 14 itens do roteiro e a cena de teste.
- Teste no PC feito pelo P3DR0EM: sistemas funcionando ("gostei muito pra um início").

### Correções após o teste
- Erro "Please initialize Cardboard XR loader..." no editor: o `CardboardStartup` (sample) foi trocado na `persistencia` pelo `CardboardBootstrap`, que só roda com o Cardboard ativo.
- A bancada de teste tinha sido salva na `persistencia`. A cena foi restaurada e o teste passou para uma cena própria, `Rooms/_Teste/teste_sistemas.unity`, com spawn automático.
- Avisos `XRSimulation duplicate ID`: diagnosticados (vêm do pacote ARCore) e registrados como pendência P1.

### Documentos
Ficam no repositório (`una-somnium/docs/`), para o Caio ver. O roteiro em PDF e as referências visuais continuam em `D:\Projetos\Somnium\docs`, fora do git.
- `docs/decisoes.md`: decisões e pendências.
- `docs/sistemas-base.md`: como usar os sistemas.
- `docs/guias/teste-sistemas-base.md`: roteiro de teste.
- `docs/notas/nota-portal-caio.md`: revisão do portal para VR.
