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

## Pendências

| # | Pendência | Dono | O que destrava |
|---|---|---|---|
| P1 | **Remover o pacote ARCore** (`com.unity.xr.arcore`)? Ele traz o AR Foundation, cujo simulador gera os avisos `XRSimulation ... duplicate ID` a cada Play. O Cardboard não depende dele. | Caio (foi ele quem adicionou) | Console limpo e build Android mais leve. |
| P2 | **Portal em estéreo**: o portal renderiza com uma câmera só; no Cardboard cada olho vê errado. Opções em `docs/notas/nota-portal-caio.md`. | Caio | Portais funcionando no celular. |
| P3 | `Portal.cs:115` usa `RenderSingleCamera`, que está obsoleto (aviso CS0618). | Caio | Evita quebra em atualização futura do URP. |
| P4 | **Reorganizar as pastas antigas** para a estrutura híbrida (um commit só, combinado). | P3DR0EM + Caio | Projeto todo no mesmo padrão. |
| P5 | **Quebras de linha (LF/CRLF)**: toda vez que o Unity abre, alguns `.asset` aparecem como modificados sem mudança real. Avaliar um `.gitattributes`. | P3DR0EM + Caio | Commits sem ruído. |
| P6 | **"Enter Play Mode Options" com o domínio sem recarregar** está ligado no projeto (Play mais rápido). Os sistemas novos já tratam isso; ficar atento a estados que "vazam" de um Play para outro. | Todos | (vigilância) |
| P7 | Testar no **celular com Cardboard + gamepad** (inventário em VR, snap turn, legibilidade dos textos). | P3DR0EM | Validar a experiência em VR. |
| P8 | Ajustes de sensação do teste no PC (velocidade, alcance, textos). Nada reportado ainda. | P3DR0EM | Ajuste fino. |
