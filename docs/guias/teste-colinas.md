# Teste da Colinas XP (branch feature/colinas-xp)

## Abrir
1. Feche e reabra o Unity, se estava aberto (a branch mudou arquivos).
2. Escolha um dos dois:
   - **Pelo Saguão (fluxo do jogo)**: **Somnium → Salas → Abrir Saguão** e Play. Vá até a porta **Colinas XP** (parede oeste, à frente e à esquerda) e `[E] Entrar: Colinas XP`.
   - **Direto**: **Somnium → Salas → Abrir Colinas XP** e Play. Você sai da porta, de frente para o campo.

## Roteiro
1. **Chegada**: vire para trás e veja a porta sozinha no meio da grama. Dê a volta nela (360°).
2. **Limite**: ande para longe. A uns 46 m da porta há uma parede invisível.
3. **Gato**: ele está à frente, um pouco à direita (laranja, sentado, sempre olhando para você). Chegue perto: ele "pisca" e reaparece longe, à sua frente. Repita algumas vezes.
4. **Manivela**: atrás da porta, à esquerda (~14 m), numa moita mais alta. Procure o **brilho**. `[E] Pegar Manivela Quadrada`.
5. Com o jogo rodando: **Somnium → Debug → Receber Lata de Ração (testar o gato)**.
6. **Gato** de novo: agora ele não foge. Mire nele: `[E] Dar ração`. O pote aparece, a chave some da coleira e você recebe a **Chave 1** e a **Metade do Medalhão**.
7. **Volta**: na porta do campo, `[E] Abrir a porta (voltar ao Saguão)`. Fade e você aparece no Saguão, na frente da porta da Colinas.
8. **Estado salvo**: entre de novo na Colinas. A manivela não está mais lá, o gato está alimentado e o pote continua ao lado dele.

## O que observar e criticar
- [ ] Sensação do campo aberto em contraste com o Saguão (escala, céu, horizonte)
- [ ] O gato: dá para perceber que ele "pisca" de propósito? Ele sempre reaparece à vista?
- [ ] Tamanho do gato (38 cm): dá para ver de longe?
- [ ] Manivela: fácil demais ou difícil demais de achar?
- [ ] Transição entre salas (fade): rápida demais? lenta demais?
- [ ] Desempenho (no PC; no celular, teste na sexta)
- [ ] Console: erros vermelhos?

## Limitações conhecidas
- Blockout: porta do Caio pintada de branco, manivela e pote em blocos, gato sem textura.
- Sem som ainda (o gato tem um campo para o miado).
- A porta é simples (fade). Ver o Saguão através dela depende do portal em estéreo (P2/P12).
