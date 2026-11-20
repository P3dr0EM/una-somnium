# Apresentação: roteiro otimizado (jogo completo, 2026-11-19)

Do Saguão até "Você acordou.", em cerca de **10 minutos** e **6 entradas em salas** (o mínimo, dadas as dependências entre os itens).

## Arquivos
- **PC (projetor)**: `D:\Projetos\Somnium\Claude\builds\Somnium_PC.zip`. Extraia a pasta e abra o `Somnium.exe` (de dentro do zip ele não abre).
- **Celular (Cardboard)**: `D:\Projetos\Somnium\Claude\builds\Android\Somnium.apk`. Instale (permitindo "fontes desconhecidas") e pareie o gamepad bluetooth antes de abrir.

## Controles
| Ação | PC | Gamepad |
|---|---|---|
| Andar / olhar | WASD + mouse | analógico esquerdo / cabeça (direito gira 30°) |
| Interagir | E ou clique | A |
| Inventário | Tab ou I | Y |
| Combinar (no inventário) | Q | X |
| Voltar / fechar | Esc | B |
| Correr | Shift | clique do analógico esquerdo |

## Mapa do Saguão
- Portas **perto do início**: **Escritório Azul** (esquerda) e **Estufa** (direita).
- Portas **ao fundo**: **Colinas XP** (esquerda) e **Salão Invertido** (direita).

## Percurso
| # | Sala | O que fazer | Ganha | Tempo |
|---|---|---|---|---|
| 0 | **Saguão** | Mostrar o hall (escadas, lustre, estátua). Ler o **bilhete** no banco da esquerda | contexto | 0:45 |
| 1 | **Colinas XP** | Virar e mostrar a porta sozinha no campo. Mostrar o **gato preto** "piscando" (siga o miado). Pegar a **Manivela** na moita atrás da porta (procure o brilho) | Manivela | 1:30 |
| 2 | **Salão Invertido** | Mostrar a sala de ponta-cabeça e a **corrente subindo do chão**. Encaixar a Manivela no **guincho**: o lustre despenca. Pegar o **Livro** nos destroços. No **inventário**, girar o Livro e achar a trava na lombada: **Alicate** | Alicate | 2:00 |
| 3 | **Estufa** | Mostrar a planta na fonte. Na **bancada** (parede esquerda), ler o bilhete e pegar o **Cartucho Vazio** | Cartucho Vazio | 1:00 |
| 4 | **Escritório Azul** | No fim do corredor central, **cortar a corrente** do alçapão: **Chave 2** e **Lata de Ração**. No **almoxarifado** (fundo à direita), abrir o armário e **encher o cartucho** no galão | Chave 2, Lata, Herbicida | 1:45 |
| 5 | **Estufa** | No **painel de irrigação** (parede direita), despejar o herbicida: a névoa cai e a planta murcha. Pegar a **Chave 3** e a **Metade B** na taça | Chave 3, Metade B | 1:00 |
| 6 | **Colinas XP** | Com a Lata no inventário, o gato fica parado. **Dar ração** | Chave 1, Metade A | 0:45 |
| 7 | **Inventário** | **Combinar** as duas metades | Medalhão do Sol | 0:20 |
| 8 | **Saguão** | Medalhão na **estátua**: pegar a **Chave 4** no alto do pedestal. Usar as 4 chaves na **Porta Principal**: **"Você acordou."** | final | 0:45 |

## O que destacar enquanto joga
- **Inventário e inspeção 3D** (passo 2, o Livro): o momento "Resident Evil".
- **Combinação de itens** (passos 4 e 7).
- **Backtracking**: Colinas e Estufa duas vezes; o estado das salas fica salvo.
- **Espaços impossíveis**: campo infinito, sala invertida, arquivo liminar, estufa estilo RE. Cada sala tem uma identidade.
- **VR**: textos e inventário legíveis no Cardboard, controle por gamepad.

## Para não travar
- O gato só para com a **Lata de Ração** no inventário. Antes disso, só mostre que ele foge.
- Esqueceu um item? Examine o objeto: a mensagem diz o que falta.
- Travou: feche e abra o jogo de novo (ele recomeça do zero). Plano B: o **.exe no projetor**.
- Ensaie o percurso inteiro antes. Meta: **menos de 10 minutos**.
