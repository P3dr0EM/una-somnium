# Salão Invertido v2: especificação (decidida em 2026-11-18, IMPLEMENTADA no mesmo dia)

Plano para refazer o Salão Invertido. Quem implementar segue este documento e `docs/notas/benchmark-salao-invertido.md`.

## Estado atual (v1)
- Gerador: `unity/Assets/Rooms/03_SalaoInvertido/Editor/SalaoBuilder.cs` (sala 10 × 14 m, altura H = 5).
- Lustre preso por 2 correntes até um mecanismo na **parede leste**. A manivela é usada lá (`LustreQueda`, `ItemLock` em `Mecanismo/Trava_Mecanismo`).
- Luz assada via `RoomKit.BakeLighting`; espaço de cor **Gamma**; intensidades calibradas para Gamma (candelabro 1,0, janelas 1,6, arandelas 2,0).

## Mudanças pedidas pelo P3DR0EM
1. **Corrente no chão**:
   - O lustre fica lá no alto, perto da mesa (no assoalho invertido). Uma única corrente **longa e fina sai do chão onde o jogador pisa e sobe esticada** até o lustre, como na referência `docs/references/dinig hall2.jpg`, invertida.
   - Objetivo: deixar o jogador em dúvida se é ele ou a sala que está de ponta-cabeça.
   - Remover o mecanismo da parede leste.
2. **Guincho no chão**:
   - A corrente sai de um **guincho de ferro** preso no chão, no centro de uma rosácea de gesso.
   - A Manivela Quadrada encaixa no guincho. Girar (catraca) afrouxa a corrente, que fica frouxa (some ou cai), e o lustre **despenca** e quebra no chão. O **Livro Antigo** fica nos destroços.
3. **Sala mais alta: H ≈ 9 m.** O lustre fica a uns 6 m do chão. Manter `LustreQueda` (queda livre ~1,1 s).
4. **Elementos das referências** (`docs/references/diningHall.jpg`, `dinig hall2.jpg`), todos coerentes com a sala invertida:
   - **arcos e pilastras góticos** em madeira escura nas paredes (de ponta-cabeça: a base das pilastras no alto e o arco ogival embaixo);
   - **estátuas brancas em nichos, de cabeça para baixo** (duas, uma de cada lado da lareira ou nas paredes laterais);
   - **hera** "crescendo" a partir do assoalho lá em cima e descendo pelas paredes, e **vasos de rosas** de ponta-cabeça (no aparador e nos cantos lá em cima).
5. Visual mais **bonito e apresentável**, no padrão da direção de arte "mansão escura" (`docs/decisoes.md`, 2026-11-18): madeira escura, tons quentes dessaturados, poças de luz, penumbra suave.

## Detalhes de implementação sugeridos
- `H = 9f`. Recalcular a mobília "grudada" no assoalho (mesa, cadeiras, aparador, lareira, relógio, tapete, lambri alto) a partir de `H`. O jogador continua andando em y = 0.
- **Lustre**: pivô no ponto mais baixo, a uns 6,0 m. Lustre maior que o atual (2 a 3 anéis de velas, como na 1ª referência), centrado sobre a rosácea e o guincho.
- **Corrente**: uma só, vertical, do guincho (y ≈ 0,4) até o fundo do lustre. Elos repetidos ou um cilindro fino. Marcar `NaoAssar`.
- **Guincho**: base de ferro, tambor horizontal com a corrente enrolada e encaixe quadrado na lateral (onde aparece a manivela).
  - `ItemLock` com `requiredItems = ManivelaQuadrada`, `usablePromptText = "Encaixar a manivela"` e `saveId = "salao/mecanismo"` (manter, para não quebrar saves).
  - `LustreQueda`: `crank` = a manivela no guincho; `chains` = a corrente; queda até y = 0. Ao soltar, a corrente pode cair junto ou sumir.
- **Destroços**: centrados no ponto de queda (em volta do guincho), com o Livro (`saveId = "salao/livro"`, com `Brilho`).
- **Luz**: o pé-direito maior pede mais luz no alto. Candelabros de parede (arandelas) em duas alturas e o lustre aceso (cúpulas emissivas; a luz do lustre não pode ser assada, porque ele cai: deixar em tempo real sob `NaoAssar`, ou sem luz própria). Recalibrar em Gamma e conferir as prévias.
- Mobile: lembrar do limite de 4 luzes por objeto em tempo real; o resto vai no bake. Manter a malha leve (blocos ou primitivos).
- Gerar com `Somnium.EditorTools.Rooms.SalaoBuilder.Build` em batch (Unity fechado), conferir as prévias em `D:\Projetos\Somnium\Claude\renders\salao_*.png` e atualizar `docs/registro.md`, `docs/decisoes.md` e `docs/guias/teste-salao.md`.
- Branch atual: `feature/ajustes-vr` (sem push). Commits pequenos; não fazer push sem perguntar.
