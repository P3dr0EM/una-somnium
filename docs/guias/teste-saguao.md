# Teste do Saguão (branch feature/saguao-blockout)

## Abrir
1. Feche e reabra o Unity, se estava aberto (a branch mudou arquivos).
2. Menu **Somnium → Salas → Abrir Saguão**. Isso abre a `persistencia` (Player) e o Saguão.
3. **Play**: você nasce na parede sul, de frente para a estátua e a Porta Principal.

## Roteiro
1. **Ambiente**: ande por todo o hall, suba as duas escadas até o mezanino e desça.
2. **Bilhete** no banco da esquerda (parede sul): `[E] Ler bilhete`.
3. **Portas laterais** (4): a dica mostra `[E] Abrir: <sala>` e aparece a mensagem de "em construção".
4. **Estátua**: `[E] Examinar a estátua` mostra a mensagem do encaixe.
5. **Porta Principal**: `[E] Examinar a porta principal` mostra "cinco fechaduras (0/5)".
6. Com o jogo rodando: **Somnium → Debug → Receber chaves 1-2-3-5 e o medalhão**.
7. **Estátua** de novo: `[E] Usar Medalhão do Sol`. O medalhão aparece no encaixe, a tampa some e a **Chave 4** aparece no alto do pedestal. Pegue.
8. **Porta Principal**: `[E] Usar Chave 1`. As chaves aparecem nas fechaduras (5/5). A porta se abre, entra luz branca, a tela clareia e aparece **"Você acordou."**. O jogo recomeça no spawn, com o inventário vazio.

Variação: insira só algumas chaves (use **Limpar inventário e estado** e depois dê só uma parte) e confira o "(3/5)".

## O que observar e criticar
- [ ] Clima: luz, cores, escala do hall (alto demais? escuro demais?)
- [ ] Escadas: sobe e desce bem? Dá para cair do mezanino ou das escadas?
- [ ] Leitura das placas das portas
- [ ] Estátua e porta principal: dá para entender o que fazer?
- [ ] Final: tempo do fade e da mensagem, sensação
- [ ] Recomeço: volta tudo ao estado inicial?
- [ ] Console: erros vermelhos?

## Limitações conhecidas
- Blockout: tudo em blocos, sem modelos ou texturas finais (pendência P10).
- O Saguão ainda não se liga ao resto do jogo (pendência P9).
- Avisos amarelos `XRSimulation` continuam (P1).
