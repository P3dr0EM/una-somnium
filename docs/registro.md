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

### Código (branch `feature/sistemas-base`, commit `fce53b6`, local, sem push)
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

### Git (fim do dia)
- O repositório local usa fork: `origin` = `P3dr0EM/una-somnium`, `upstream` = `ocai0/una-somnium` (Caio). As atualizações do Caio vêm de `git fetch upstream`.
- P3DR0EM agora é colaborador no repositório do Caio.
- **PR #1** (`feature/sistemas-base` → `main`): https://github.com/ocai0/una-somnium/pull/1. Merge feito pelo GitHub.

### Próximos passos
1. Mecânicas do roteiro: teclado numérico do cofre, gato (foge / é atraído pela ração), arquivo de metal empurrável, lustre que cai.
2. Normalizar a escala e o pivô dos modelos do Blender e exportar em FBX.
3. Save em disco (inventário + WorldState) com pontos de save no estilo RE.
4. Teste no celular com Cardboard + gamepad (pendência P7).
5. Combinar com o Caio as pendências P1 a P5 (`decisoes.md`).

## 2026-11-12

### Início
- Sincronizado com o Caio: nada novo desde o merge do PR #1. Branch **`feature/saguao-blockout`** criada a partir da `main`.

### Quebras de linha (P5 resolvida)
- `.gitattributes` na raiz: arquivos serializados do Unity como `-text` (o git guarda os bytes como o Unity escreve) e binários marcados.
- Cópias locais regravadas uma vez. Depois de 4 execuções do Unity, nenhum arquivo apareceu como modificado sem motivo.

### Saguão central (blockout jogável)
- Gerador `Rooms/00_Saguao/Editor/SaguaoBuilder.cs`, com os menus **Somnium → Salas → Abrir Saguão / Gerar blockout / Renderizar prévias**.
- Cena `Rooms/00_Saguao/saguao.unity` (adicionada à lista de cenas do build):
  - hall de 14,4 × 16,8 m e pé-direito de 7 m;
  - piso xadrez em 28 placas (limite de 4 luzes por objeto no celular);
  - escadaria dupla e mezanino com guarda-corpo;
  - lustre e 6 arandelas; tapete, bancos, quadros, vasos e um bilhete.
- **Porta Principal**: 5 fechaduras (`ItemLock` com inserção parcial), chaves que aparecem nas fechaduras conforme são inseridas (`LockProgressIndicator`) e final do jogo (`GameEnding`).
- **Estátua**: o Medalhão do Sol encaixa no pedestal, a tampa some e a Chave 4 aparece.
- **4 portas** com placas (Colinas XP, Escritório Azul, Salão Invertido, Estufa), trancadas com a mensagem "em construção" (`MessageInteractable`).
- Prévias renderizadas pelo Unity em batch, revisadas e corrigidas: iluminação reforçada perto do spawn e chão claro fora da porta no final.

### Sistemas novos ou alterados
- `ItemLock`: evento `onProgressChanged(int)` e `InsertedCount()` público.
- `Itr_Door` (Caio): opção `interactableByPlayer` (o padrão continua `true`, sem mudança para as portas existentes).
- `Inventory.Clear()`.
- Novos: `LockProgressIndicator`, `MessageInteractable`, `ScreenFader` (fade de tela inteira, também no VR), `GameEnding` (final + recomeço), `RoomSettings` (luz da sala).
- Menu **Somnium → Debug** (só no Play): receber chaves, receber todos os itens, limpar o estado.

### Teste do P3DR0EM e ajuste da escada
- Teste no PC: tudo funcionou (estátua, chaves, final com recomeço, luz e ambientação). Console sem erros.
- A subida da escada estava aos trancos (o jogador "pulava" degrau por degrau pelo `stepOffset`). Correção: os degraus viraram só visuais e cada escada ganhou uma **rampa invisível** (`Rampa_Colisao`, ~38,7°, abaixo do `slopeLimit` de 45°) passando pelas quinas dos degraus. O último degrau mantém a colisão para emendar com o mezanino.

### Fechamento do dia
- **PR #2 aceito** (merge `b9b0b05`): Saguão, final do jogo e `.gitattributes` na `main`.
- Atribuição automática do Claude nos commits/PRs **desligada** (configuração local do P3DR0EM).
- **Planejamento até a apresentação de 23/11** e redução de escopo (ver `decisoes.md`, 2026-11-12):
  - Sala das Piscinas descartada.
  - Fluxo enxuto com 4 salas e 4 chaves.
  - Conceito novo da Colinas XP (porta isolada no campo).
  - Gato que se teletransporta.
- Mensagem com o status e a lista de modelos por prioridade enviada ao grupo.

## 2026-11-14

### Início
- Sincronizado com o Caio: nada novo. Branch **`feature/colinas-xp`** criada a partir da `main`.
- A sessão de sexta (13) não aconteceu; cronograma ajustado (ver "Cronograma até a apresentação" em `decisoes.md`).

### Colinas XP (blockout jogável)
- Gerador `Rooms/01_ColinasXP/Editor/ColinasBuilder.cs`, com os menus **Somnium → Salas → Abrir Colinas XP / Gerar blockout / Renderizar prévias**.
- Cena `Rooms/01_ColinasXP/colinas_xp.unity` (em x = 2000):
  - campo de colinas gerado por código (360 × 360 m, ~16 mil triângulos), plano perto da porta e com bordas que sobem para fechar o horizonte;
  - textura de grama estilizada e tufos 3D só num raio de 16 m (uma malha só, ~4 mil triângulos);
  - céu panorâmico próprio com nuvens paradas, neblina azul-clara e sol com sombra.
- **Porta**: a do Caio, tirada do prefab do portal (folha fechada e pintada de branco), com tijolos e hera. Interagir leva de volta ao Saguão.
- **Gato**: modelo low poly sentado (CC BY), com coleira e chave. Se teletransporta (`GatoTeleporte`); com a Lata de Ração, "Dar ração" entrega a Chave 1 e a Metade do Medalhão e o pote aparece.
- **Manivela Quadrada** numa moita atrás da porta, com um brilho que pisca.
- Limite invisível a 46 m da porta.

### Passagem entre salas
- Novos `RoomTransition` e `RoomDoor`; `PlayerSpawnPoint` ganhou `id` e `placeOnStart`.
- No Saguão, a porta "Colinas XP" agora funciona. Há um ponto de chegada na volta e a Porta Principal passou para **4 fechaduras** (fluxo enxuto); o bilhete fala em "quatro portas, quatro chaves".

### Outros
- `pote_de_racao.blend` (em `models/`) ainda tem 3 variações lado a lado, com ~2 m. Por isso o pote do blockout é de blocos.
- O `.blend` dos modelos não foi tocado. `Galão`, `escrivaninha` e `estatua` têm alterações locais do grupo, fora deste commit.
