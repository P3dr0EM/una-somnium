# Teste dos sistemas base (branch feature/sistemas-base)

## 1. Abrir o projeto
1. Unity Hub → **Projects** → **Add** → **Add project from disk** → escolha `D:\Projetos\Somnium\una-somnium\unity`.
2. Confira se a coluna *Editor Version* mostra **6000.6.7f1**. Se o Hub perguntar, escolha essa versão.
3. Abra o projeto (a primeira importação já foi feita, deve abrir em 1 a 2 min).
4. Se aparecer aviso de **Safe Mode**, não entre: feche e me avise.

## 2. Preparar a cena
1. Menu **Somnium → Teste → Abrir cena de teste**. Isso abre a `persistencia` (onde está o Player) e, junto, a cena `Assets/Rooms/_Teste/teste_sistemas.unity` (chão, luz e bancada, longe da área externa).
2. Aperte **Play**: o jogador nasce sozinho na frente da bancada (componente `PlayerSpawnPoint`).
3. A bancada vive só na cena de teste. **Não copie objetos dela para a `persistencia`.**
4. Para voltar ao estado original da cena de teste: **Somnium → Teste → Recriar cena de teste**.

## 3. Controles

| Ação | Teclado/mouse | Gamepad |
|---|---|---|
| Andar / correr | WASD / Shift | Analógico E / clicar analógico E |
| Olhar | Mouse | Analógico D |
| Pular | Espaço | B |
| Interagir | E ou clique esquerdo | A |
| Inventário (abrir/fechar) | Tab ou I | Y |
| Combinar (no inventário) | Q | X |
| Voltar / cancelar (no inventário) | Esc | B |
| Girar item (no inventário) | Mouse | Analógico D |

Atenção: no editor, o Esc também solta o mouse do Game View. Para fechar o inventário, prefira Tab. Se o mouse soltar, clique no Game View.

## 4. Roteiro de teste
Aperte **Play** e siga a ordem:

1. **Andar e olhar**: ande pela área, corra, pule.
2. **Mira**: olhe para o livro na mesa. Deve aparecer `[E] Pegar Livro Antigo` e a mira cresce.
3. **Pegar**: aperte E. O livro some e aparece `Obteve: Livro Antigo`.
4. **Inventário**: aperte Tab. O jogo pausa, o fundo fica escuro e aparecem a grade e o livro (vermelho) girando com o mouse.
5. **Segredo**: gire o livro até a lombada (o lado estreito com uma peça dourada) ficar de frente. A dica muda para `[E] Pressionar a trava na lombada`. Aperte E e você ganha o **Alicate**.
6. **Medalhão**: feche (Tab), pegue as duas metades (discos dourados), abra o inventário, selecione uma metade, aperte **Q**, vá até a outra e aperte **Q** de novo. Deve virar **Medalhão do Sol**.
7. **Estátua**: olhe para o bloco cinza. A dica deve ser `[E] Usar Medalhão do Sol`. Use e você ganha a **Chave 4**.
8. **Corrente**: antes de ter o alicate, a dica é "Examinar corrente" e mostra a mensagem de que precisa cortar. Com o alicate, a corrente some.
9. **Porta do Caio** (na área externa): mire e veja se aparece `[E] Abrir` / `[E] Fechar`.
10. **Portal do Caio**: atravesse e veja se o teleporte acontece sem tranco.
11. **Gamepad** (se tiver um conectado no PC): repita os passos 2 a 7 pelo controle. As dicas devem trocar para A/Y/X/B.

## 5. O que observar e criticar
Anote o número do item e o que sentiu. Valem impressões ("lento", "confuso", "feio").

**Movimento**
- [ ] Velocidade de andar (3 m/s) e correr (5 m/s)
- [ ] Altura do pulo (1,1 m)
- [ ] Sensibilidade do mouse e do analógico

**Mira e mensagens**
- [ ] Alcance para interagir (2,5 m): longe ou perto demais?
- [ ] Tamanho e legibilidade da dica e das mensagens
- [ ] Tempo das mensagens na tela (3 s)
- [ ] Textos em português claros, no tom do jogo?

**Inventário**
- [ ] Layout: grade à esquerda, item à direita, legibilidade
- [ ] Velocidade de girar o item
- [ ] Achar o segredo do livro: fácil, difícil, óbvio demais?
- [ ] Fluxo de combinar (Q, Q) intuitivo?
- [ ] Pausar o jogo com o inventário aberto: ok?

**Geral**
- [ ] Algum erro (vermelho) ou aviso `[GameInput]` no **Console** (Window → General → Console)?
- [ ] Algo travou, sumiu ou se comportou estranho?

## 6. Limitações conhecidas (não precisa reportar)
- Itens sem modelo 3D aparecem como cubo no inventário (por exemplo, o medalhão).
- A fonte é a padrão da Unity, provisória.
- Sem sons ainda.
- A cena de teste não descarrega durante o jogo, então ela não testa o "lembrar estado ao recarregar a sala".
- Avisos amarelos `XRSimulation ... duplicate ID` no Console: vêm do pacote ARCore, ver pendência P1 em `docs/decisoes.md`.
- Ainda não testado no celular/Cardboard.
