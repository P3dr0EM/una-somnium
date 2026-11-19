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

### Teste do P3DR0EM na Colinas XP e ajustes
- **Retorno:**
  - campo aberto muito confortável em VR;
  - tamanho do gato bom; manivela com boa dificuldade;
  - transição e desempenho no PC ideais.
- **Bug corrigido:** ao voltar a uma sala, o inventário aparecia vazio e o Console mostrava `MissingReferenceException ... Image`.
  - Causa: a raiz da UI do inventário era criada sem pai, ou seja, na **cena ativa** (a sala), e era destruída quando a sala era descarregada. Os itens nunca se perderam.
  - Correção: `InventoryUI` move a raiz para a cena do Player (`persistencia`).
  - Regra: **objeto criado em tempo de execução precisa de pai, ou ir para a cena da persistencia, ou ser `DontDestroyOnLoad`**.
- **Gato:** agora **preto, com coleira azul**. Contraste forte com o verde e combinação legível nos tipos comuns de daltonismo; o laranja se confundia com o verde.
- **Miado** sintetizado por código (`Rooms/01_ColinasXP/Audio/miado.wav`, sem licença de terceiros), em **som 3D** (vem da direção do gato):
  - toca quando o gato pisca e a cada 8 a 15 s, com o tom variando;
  - para depois de alimentado.
- **PR #3 aceito** (merge `cc43f17`): Colinas XP na `main`.

## 2026-11-14 (noite)

### Escritório Azul (blockout jogável)
- Branch **`feature/escritorio-azul`**. Gerador `Rooms/02_EscritorioAzul/Editor/EscritorioBuilder.cs`, com os menus **Somnium → Salas → Abrir / Gerar blockout / Renderizar prévias do Escritório Azul**.
- Cena `Rooms/02_EscritorioAzul/escritorio_azul.unity` (em x = 3000): "arquivo liminar" de 12 × 22 m e pé-direito de 3 m, tudo em azul.
  - 6 fileiras de arquivos de metal formando corredores (uma malha de 6 arquivos reaproveitada, ~15 mil triângulos no total).
  - Mesas e cadeiras (**modelos do grupo**: `escrivaninha.blend` e `cadeira.blend`, exportados para FBX sem alterar os `.blend`), máquinas de escrever.
  - Luz fluorescente fria (11 pontos, sem sombra) e **uma lâmpada piscando** (`LuzPiscando`).
- **Alçapão**:
  - O Alicate corta a corrente e a tampa abre (`GirarAoAtivar`).
  - No nicho estão a **Chave 2** (com brilho) e a **Lata de Ração**.
  - Uma grade invisível (layer Ignore Raycast) impede o jogador de cair no nicho sem bloquear a mira.
- **Almoxarifado**: armário com duas portas (`Itr_Door` do Caio), com o **galão de ácido** (modelo do grupo `Galão.blend`) e rótulo amarelo "PERIGO ÁCIDO". Cartucho Vazio vira **Herbicida**.
- **Memorando** numa mesa com a pista do alçapão.
- Saguão: a porta "Escritório Azul" agora leva à sala (tabela `RoomLinks` no `SaguaoBuilder`).

### Sistemas novos ou alterados
- `RoomKit` (`_Core/Editor`): funções comuns dos geradores de sala (blocos, materiais, modelos com escala automática, piso com UV em metros, textos, prévias).
- `GirarAoAtivar` e `LuzPiscando`.
- `PlayerInteractor`: gatilhos que não aceitam interação (ex.: trava já aberta) não bloqueiam mais a mira.
- Debug: **Receber Alicate e Cartucho Vazio**.
- **Correção visual:** borda do alçapão "serrilhada" (z-fighting). O topo das paredes do nicho ficava na mesma altura do piso; agora termina 1 cm abaixo.
- **PR #4 aceito** (merge `a85aeb7`): Escritório Azul na `main`.

## 2026-11-15

### Versão de demonstração (APK + PC) para mostrar em aula
- Branch **`feature/demo-apk`**.
- **Início no Saguão** (P9, por enquanto): a `persistencia` carrega o `saguao` em vez da `area_externa`.
  - O `LevelPersistencia` (do Caio) só carrega a sala inicial quando a `persistencia` está sozinha. No editor, qualquer cena aberta junto (uma sala ou a `area_externa`) é respeitada.
- **Mesa de demonstração** no Saguão, à direita do início, com os itens das salas que ainda não existem:
  - Livro Antigo (a inspeção 3D revela o Alicate);
  - Cartucho Vazio, Metade B do Medalhão e Chave 3.
  - Dá para jogar o percurso inteiro até o final. Desligar com `DemoTable = false` no `SaguaoBuilder` quando as salas existirem.
- **Builds**: menu **Somnium → Build** (`SomniumBuild.cs`), saída em `D:\Projetos\Somnium\Claude\builds`.
  - PC: `PC\Somnium.exe` (111 MB).
  - Android: `Android\Somnium.apk` (58 MB, IL2CPP, ARMv7 + ARM64, Cardboard).
- Roteiro da apresentação: `docs/guias/demo-apresentacao.md`.

### Problemas do build Android e soluções
1. **Build interrompido no meio do Gradle** (a sessão caiu). O cache do Gradle ficou corrompido.
2. **"Using different versions of the Android Gradle plugin (UNKNOWN_VERSION, UNKNOWN_VERSION)"**: persistiu mesmo com o cache apagado.
   - Um projeto Android mínimo reproduziu o erro com a pasta do Gradle da máquina e funcionou com uma pasta nova. A causa está na pasta pessoal do Gradle (`D:\Unity\Cache\gradle`, compartilhada com o Flutter), e não no projeto.
   - Solução: pasta do Gradle nova. A antiga ficou em `D:\Unity\Cache\gradle_bak_20261115` (pode ser apagada).
3. **"resource style/Theme.AppCompat.NoActionBar not found"** (Cardboard). Os templates de Gradle (`mainTemplate.gradle`, com as dependências do Cardboard: appcompat, play-services-vision, material, protobuf) tinham ido parar em `Assets/extras/Plugins/Android` na reorganização de pastas.
   - O Unity só lê templates em **`Assets/Plugins/Android`**. Os arquivos voltaram para lá.
4. Os builds alteram sozinhos `ProjectSettings` e os assets do URP (XR nos "preloaded assets", filtros de shader). Essas mudanças foram descartadas.
- **PR #5 aceito** (merge `7cba40b`): versão de demonstração na `main`.

## 2026-11-18

### Retorno da apresentação (APK no Cardboard)
- Experiência boa no geral; mecânicas, inventário e textos funcionam bem em VR.
- **Câmera baixa ("visão de criança")**. Causa: o `TrackedPoseDriver` da câmera (na `persistencia`) estava em "posição e rotação". O Cardboard só rastreia rotação, e a posição da cabeça zerava a altura da câmera (~0,95 m em vez de ~1,76 m). Correção: **só rotação** (`m_TrackingType: 1`). No PC não aparecia, porque sem headset o driver não mexe na posição.
- **Iluminação do Saguão ruim no celular**. Pelos prints:
  - *banding* forte: anéis de cor com faixas verde-oliva e rosa sobre o vermelho, perto das arandelas e do lustre;
  - sem sombras, e contraste extremo.
- Correções:
  - `Mobile_RPAsset`: **HDR desligado** e **conversão sRGB aproximada desligada** (precisão de cor nos escuros).
  - **Saguão com luz assada (baked)**: lightmaps com sombras macias, rebatimento e oclusão, mais Light Probes (126) para o que se move e uma sonda de reflexo para os metais. Bake em ~6 s.
  - Mais luz: lustre 7 → 11, luz de preenchimento no teto, arandelas 2,2 → 3,2, ambiente e teto mais claros, neblina mais leve.
- `RoomKit.BakeLighting(...)`: usado pelas próximas salas.
- Ajuste do P3DR0EM no `Brilho`: pisca mais vezes (intervalo de 1,7 s, duração de 0,6 s).

### Salão Invertido (blockout jogável, luz assada)
- Gerador `Rooms/03_SalaoInvertido/Editor/SalaoBuilder.cs`, com os menus **Somnium → Salas → Abrir / Gerar / Renderizar prévias do Salão Invertido**. Cena em x = 4000.
- Sala de jantar vitoriana de ponta-cabeça: o jogador anda no forro de gesso; assoalho, tapete, mesa posta, cadeiras, aparador, lareira e relógio ficam lá em cima.
  - O lambri de madeira fica no alto, e a sanca de gesso junto ao chão.
- Detalhes surreais:
  - **lareira com o fogo para baixo** (`FogoTremulo`, luz em tempo real);
  - **velas invertidas** (candelabro e arandelas);
  - **janelas com o céu embaixo** (textura gerada);
  - **retratos de cabeça para baixo**;
  - **relógio com o pêndulo para cima** (`PenduloInvertido`);
  - a porta tem a maçaneta e a placa de ponta-cabeça.
- **Puzzle**:
  - **Manivela Quadrada** no mecanismo da parede leste: a manivela aparece e gira, as correntes somem e o lustre cai em queda livre e quebra (`LustreQueda`).
  - Nos destroços está o **Livro Antigo** (inspeção 3D → Alicate).
  - Sons sintetizados: catraca e vidro quebrando.
- Novo `NaoAssar` (marca objetos que se movem para o bake ignorar). O bake do `RoomKit` também deixa em tempo real as luzes desses objetos (o fogo).
- A **mesa de demonstração** do Saguão não tem mais o Livro. A porta do Salão no Saguão agora funciona.
- Debug: **Receber Manivela Quadrada**.

### Cor e iluminação no Cardboard (2º teste)
- **Altura da câmera ok** depois da correção.
- **Cores ainda ruins no Saguão.** Os prints mostraram *posterização* forte, mesmo com a luz assada: poucos tons, faixas verde-oliva e amarelas no vermelho. Diagnóstico:
  - projeto em espaço de cor **Linear** gravando em 8 bits no Cardboard: sobram pouquíssimos tons nos escuros;
  - em vermelho escuro, o verde e o azul "pulam" de tom em momentos diferentes.
- **Correção: espaço de cor Gamma** (padrão em VR mobile).
  - Todas as salas regeradas e reassadas.
  - Novo menu **Somnium → Salas → Gerar TODAS as salas** (`TodasSalas.cs`).
- **Direção de arte do Saguão** seguindo as referências do P3DR0EM (mansão de Resident Evil, `docs/references/reference1.jpg`, `reference5.webp`):
  - lambri de madeira escura até 1,4 m com friso;
  - papel de parede vinho-amarronzado dessaturado;
  - tapete verde-escuro; mármore envelhecido; teto de madeira;
  - **cúpulas acesas no lustre** (ponto focal);
  - penumbra quente com poças de luz.
- Intensidades recalibradas para Gamma:
  - Saguão: lustre 6,5, teto 1,6, arandelas 1,9, ambiente 0,14;
  - Salão: candelabro 1,0, janelas 1,6;
  - rebatimento 1,1.
- Colinas XP em Gamma: grama menos saturada, sol 1,15, porta menos branca.
- **Retorno do 3º teste:** cores muito melhores no Cardboard (Gamma).
- Pedido de **Salão Invertido v2**: corrente saindo do chão e subindo até o lustre (dúvida "quem está de ponta-cabeça"), guincho no chão, sala de ~9 m, arcos e pilastras góticos, estátuas invertidas, hera e rosas. Benchmark em `docs/notas/benchmark-salao-invertido.md`; especificação em `docs/notas/salao-invertido-v2-spec.md`.

### Salão Invertido v2
- **Pé-direito de 9 m**: a mobília lá em cima e o lustre a ~6 m (queda de ~1,1 s).
- **A corrente sobe do chão.** Ela sai de uma **roldana** no centro da rosácea e sobe esticada até o lustre, o elemento "impossível" que deixa a dúvida sobre quem está de ponta-cabeça. No chão, ela corre até o **guincho**, a 2 m.
  - A Manivela vai no guincho, e o lustre não cai em cima de quem gira (conforto em VR).
- Lustre de ponta-cabeça: gancho embaixo, dois anéis vazados, velas pendendo com a chama para baixo e cristais para cima; luz própria em tempo real (some quando cai).
- Elementos das referências (`diningHall.jpg`, `dinig hall2.jpg`):
  - pilastras e **arcos ogivais invertidos** (capitel embaixo, base em cima, arco com a ponta para baixo);
  - **estátuas de cabeça para baixo** em nichos ao lado da lareira;
  - janelas altas com arco embaixo;
  - **hera** descendo do assoalho lá em cima (uma malha combinada);
  - **rosas** em vasos invertidos nos cantos.
- Arandelas em dois níveis nas pilastras.
- Dourado e ferro **foscos** (metal puro fica preto com luz assada).
- Benchmark de jogos de referência: `docs/notas/benchmark-salao-invertido.md`.
