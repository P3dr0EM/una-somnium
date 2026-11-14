# Somnium: decisões e pendências

Registro das decisões tomadas sobre o roteiro (rascunho) e o projeto. Cada decisão tem data e motivo. Pendências ficam no fim, com dono e o que destrava cada uma.

## Plataforma e controles

| Data | Decisão | Motivo |
|---|---|---|
| 2026-11-11 | Unity **6000.6.7f1**, URP, Input System, Google Cardboard (Android). | Versão em que o projeto foi criado; os dois devs usam a mesma. |
| 2026-11-11 | Apresentação no **PC com mouse e teclado**; no celular, **Cardboard + gamepad bluetooth**. | Cardboard sozinho tem só um botão; o gamepad permite andar e usar menus. |
| 2026-11-11 | Botões: interagir **E / clique / A**; pular **Espaço / B**; inventário **Tab / I / Y**; combinar **Q / X**; voltar **Esc / B**; correr **Shift / clique do analógico E**. | Padrão mais comum hoje (A = ação principal). |
| 2026-11-11 | No VR a cabeça controla o olhar; o analógico direito gira o corpo **em passos de 30°**. Andar é relativo ao olhar. | Reduz enjoo em VR. |
| 2026-11-11 | Velocidades: andar **3 m/s**, correr **5 m/s**, pulo **1,1 m**. | Valores confortáveis em VR (antes: 12 m/s e 3 m). |

## Design (ajustes sobre o roteiro)

| Data | Decisão | Motivo |
|---|---|---|
| 2026-11-11 | **Salão Invertido**: a sala é modelada de ponta-cabeça e a gravidade é normal para tudo. Lustre e cofre ficam "acima" do jogador e caem na direção dele. | Mais simples e legível do que inverter a gravidade. |
| 2026-11-11 | **Sem ameaça/inimigo.** Escape room puro; o lado Resident Evil fica em inventário, combinação, inspeção 3D e backtracking. | Escopo e conforto em VR. |
| 2026-11-11 | **Alçapão**: mantém o pulo do roteiro, mas com protótipo cedo para testar o conforto no Cardboard. | Pulo e plataforma podem enjoar em VR. |
| 2026-11-11 | **Uso de itens automático**: se o jogador tem o item certo, interagir com o objeto já usa. Sem o item, aparece uma dica. | Mais fluido com gamepad em VR. |
| 2026-11-11 | **Inventário**: painel flutuante com o jogo pausado e o item em 3D girável (inspeção) no próprio painel. **Sem limite de espaço.** | Poucos itens (~15) no jogo; o foco é nos puzzles. |
| 2026-11-12 | **Saguão** é o primeiro ambiente montado (blockout jogável), com estética de **hall de mansão decadente**: piso xadrez, escadaria dupla com mezanino, lustre, papel de parede gasto. | É o hub que liga as salas e onde o jogo começa e termina. |
| 2026-11-12 | O Saguão é uma **cena própria** (`Rooms/00_Saguao/saguao.unity`), afastada da área externa (x = 1000). A ligação com o prédio e os portais do Caio fica para combinar. | Não mexer nas cenas do Caio. |
| 2026-11-12 | As portas para as outras salas ficam **trancadas com placa** ("Sala em construção") até cada sala existir. | Blockout jogável sem depender das outras salas. |
| 2026-11-12 | **Final do jogo**: as 5 chaves na Porta Principal abrem a porta, entra luz forte, a tela clareia até o branco, aparece **"Você acordou."** e o jogo recomeça. | Tema do sonho (Somnium). |
| 2026-11-12 | A **Chave 4** aparece numa tampa no alto do pedestal da estátua ao encaixar o Medalhão do Sol (o jogador pega com a mira). | Leitura do roteiro: "a estátua abre revelando a Chave 4". |
| 2026-11-12 | **Escadas sobem por rampa invisível**: degraus só visuais e um colisor em rampa por cima. Padrão para todas as escadas do jogo. | Subida suave, sem trancos de degrau em degrau (mais confortável em VR). |
| 2026-11-12 | **Sala das Piscinas descartada** (decisão do grupo). | Escopo para a apresentação de 23/11. |
| 2026-11-12 | **Fluxo enxuto (opção B): 4 salas e 4 chaves.** Saem o código nas nuvens, o cofre com teclado (antiga Chave 5), o pulo pelo alçapão e o arquivo empurrado. A Porta Principal passa a ter **4 fechaduras**. Fluxo completo abaixo. | Cabe no prazo e corta o sistema mais arriscado em VR (digitar código com gamepad). |
| 2026-11-12 | **Colinas XP**: a porta e o batente ficam **sozinhos no meio de um campo verde infinito** (estilo papel de parede do XP + porta de Suzume), com 360° de circulação. No roteiro original, as paredes mostravam que era um cenário fechado; isso foi substituído. Céu e colinas são **próprios** (não usar a foto *Bliss*, que tem direitos). | Ideia do P3DR0EM e do Caio: contraste forte entre a sala fechada e o espaço aberto em VR. |
| 2026-11-12 | A porta da Colinas XP começa **simples** (atravessar = fade e volta ao Saguão). Se o portal em estéreo (P2) for resolvido, ela vira portal com vista do Saguão. | Não deixar a sala dependente de um problema técnico. |
| 2026-11-12 | **Gato**: sem animação. Quando o jogador chega a ~3 m, ele **se teletransporta** ("pisca") para um ponto a até 30 m da porta, a pelo menos ~10 m do jogador e **dentro do campo de visão** dele. Com a Lata de Ração no inventário, ele fica parado; mirar mostra "Dar ração" e interagir entrega a **Chave 1** e a **Metade do Medalhão** (o pote de ração aparece ao lado dele). | Ideia do P3DR0EM: poupa a animação e combina com o clima de sonho; reaparecer à vista evita o jogador perder o gato. |
| 2026-11-12 | Modelo do gato: **low poly sentado** (428 triângulos, licença **CC BY**, crédito em `CREDITOS.md`). O projeto é **acadêmico, sem venda**. | Leve para o celular; a pose sentada combina com o teletransporte. |

### Fluxo de gameplay (opção B)
1. Começa no **Saguão**, de frente para a Porta Principal (4 fechaduras).
2. **Colinas XP**: o gato com a Chave 1 se teletransporta para longe; na grama está a **Manivela Quadrada**.
3. **Salão Invertido**: a Manivela no mecanismo da parede derruba o lustre, que revela o **Livro Antigo**; inspecionado em 3D, o Livro tem um fundo falso com o **Alicate**.
4. **Escritório Azul**: o Alicate corta a corrente do alçapão; no nicho estão a **Chave 2** e a **Lata de Ração**; na sala fica o galão de ácido.
5. **Colinas XP**: dar a ração ao gato dá a **Chave 1** e a **Metade do Medalhão**.
6. **Estufa**: a planta bloqueia a Chave 3; ali está o **Cartucho Vazio**. Cartucho + ácido (no Escritório) = **Herbicida**; nos sprinklers, a planta murcha e libera a **Chave 3** e a **outra Metade**.
7. As duas metades formam o **Medalhão do Sol**; na estátua do Saguão, ele libera a **Chave 4**.
8. Com as 4 chaves, a Porta Principal se abre: "Você acordou."

### Cronograma até a apresentação (23/11)
Revisto em 2026-11-14 porque a sessão de sexta (13) não aconteceu. O domingo, 22, deixou de ser folga.

| Dia | Foco |
|---|---|
| Sáb 14 | Colinas XP + passagem entre salas (portas do Saguão) |
| Dom 15 | Escritório Azul |
| Qua 18 | Salão Invertido |
| Qui 19 | Estufa |
| Sex 20 | Ponta a ponta com o Caio (P9) + teste no celular |
| Sáb 21 | Modelos e acabamento |
| Dom 22 | Versão congelada, correções e ensaio |

## Projeto e organização

| Data | Decisão | Motivo |
|---|---|---|
| 2026-11-11 | Trabalho em **branches** (`feature/...`); sincronizar com o remoto antes de começar; nada direto na `main`. | O Caio e o P3DR0EM trabalham em paralelo. |
| 2026-11-11 | Estrutura de pastas **híbrida**: `_Core/` (sistemas globais), `Mechanics/<Nome>/` (mecânicas), `Rooms/<NN_Nome>/` (sala: cena e arte própria), `Shared/` (arte comum), `Data/` (ScriptableObjects), `extras/` (terceiros). | Estende o padrão que o Caio começou; escala para as 5 salas e reduz conflitos. |
| 2026-11-11 | **Só código novo** segue a estrutura. Arquivos existentes (`Scripts/`, `Scenes/`, `Parts/`, `Portal`) só serão movidos num commit de reorganização combinado com o Caio. | Mover arquivos enquanto o outro trabalha gera conflito. |
| 2026-11-11 | Testes de sistema rodam numa **cena de teste separada** (`Rooms/_Teste/teste_sistemas.unity`), aberta junto da `persistencia`. Nada de teste vai para cenas compartilhadas. | A bancada de teste tinha sido salva por engano na `persistencia`. |
| 2026-11-11 | O script de exemplo `CardboardStartup` foi trocado na `persistencia` por `Somnium.Core.CardboardBootstrap`, que só roda com o Cardboard ativo. | O exemplo gerava erro a cada frame no editor/PC. |
| 2026-11-11 | Backups do Blender (`*.blend1`, `*.blend2`) ficam fora do git (`.gitignore` na raiz). | Arquivos automáticos que só incham o repositório. |
| 2026-11-11 | Caches do Unity e do Gradle ficam no **D:** (`D:\Unity\Cache`), via variáveis de ambiente do usuário. | O C: está quase cheio. |
| 2026-11-12 | **`.gitattributes`**: arquivos que o Unity grava (cenas, prefabs, `.asset`, `.meta`, ProjectSettings) ficam como `-text`, sem conversão de quebra de linha. Scripts não foram afetados. | Resolve a P5: arquivos não aparecem mais como modificados sem mudança real. |
| 2026-11-12 | Salas são geradas por **scripts de editor** (`Rooms/<sala>/Editor/...Builder.cs`) e depois editadas à mão. O gerador também renderiza **prévias em PNG**. | Blockout reproduzível e verificável sem abrir o editor. |
| 2026-11-12 | Cada sala tem um **`RoomSettings`**, que torna a cena dela a ativa ao carregar (vale a iluminação ambiente e a neblina da sala). | A `persistencia` não deve ditar a luz de todas as salas. |

## Pendências

| # | Pendência | Dono | O que destrava |
|---|---|---|---|
| P1 | **Remover o pacote ARCore** (`com.unity.xr.arcore`)? Ele traz o AR Foundation, cujo simulador gera os avisos `XRSimulation ... duplicate ID` a cada Play. O Cardboard não depende dele. | Caio (foi ele quem adicionou) | Console limpo e build Android mais leve. |
| P2 | **Portal em estéreo**: o portal renderiza com uma câmera só; no Cardboard cada olho vê errado. Opções em `docs/notas/nota-portal-caio.md`. | Caio | Portais funcionando no celular. |
| P3 | `Portal.cs:115` usa `RenderSingleCamera`, que está obsoleto (aviso CS0618). | Caio | Evita quebra em atualização futura do URP. |
| P4 | **Reorganizar as pastas antigas** para a estrutura híbrida (um commit só, combinado). | P3DR0EM + Caio | Projeto todo no mesmo padrão. |
| ~~P5~~ | ~~Quebras de linha (LF/CRLF)~~. **Resolvida em 2026-11-12** com `.gitattributes`. Se o Caio vir arquivos "modificados" sem mudança depois de puxar, basta um `git checkout -- .` (com o Unity fechado e sem alterações pendentes). | – | – |
| P6 | **"Enter Play Mode Options" com o domínio sem recarregar** está ligado no projeto (Play mais rápido). Os sistemas novos já tratam isso; ficar atento a estados que "vazam" de um Play para outro. | Todos | (vigilância) |
| P7 | Testar no **celular com Cardboard + gamepad** (inventário em VR, snap turn, legibilidade dos textos). | P3DR0EM | Validar a experiência em VR. |
| P8 | Ajustes de sensação do teste no PC (velocidade, alcance, textos). Nada reportado ainda. | P3DR0EM | Ajuste fino. |
| P9 | **Como o jogador chega ao Saguão?** Hoje ele é testado sozinho (spawn próprio). Opções: o `LevelPersistencia` carregar o Saguão como sala inicial, ou um portal/porta da área externa levar até ele. | P3DR0EM + Caio | Fluxo do jogo de ponta a ponta. |
| P10 | **Arte final do Saguão**: trocar os blocos por modelos (estátua: há um `estatua.blend` em `models/`), texturas de papel de parede e madeira, iluminação baked para o celular. | P3DR0EM + Caio | Visual final. |
| P11 | **Crédito do gato (CC BY)**: preencher autor e link em `CREDITOS.md`. | P3DR0EM | Cumprir a licença. |
| P12 | **Porta da Colinas XP como portal** (ver o Saguão através dela). Depende da P2. | Caio | Efeito "Suzume" completo. |
