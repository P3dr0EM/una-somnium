# Benchmark: salas invertidas, gravidade e espaços impossíveis (para o Salão Invertido)

Levantamento de 2026-11-18. Foco em jogos feitos em **Unity** com salas invertidas, gravidade estranha, ilusão de perspectiva ou espaços impossíveis, principalmente em VR.
Onde a pesquisa **não confirmou** o motor, isso está indicado.

## Jogos de referência

| Jogo | Motor | O que tem a ver com o Salão Invertido | Lição para nós |
|---|---|---|---|
| **Manifold Garden** (William Chyr, 2019) | Unity (confirmado) | Gravidade que muda conforme a parede em que você pisa; arquitetura "impossível" que se repete ao infinito. | A **dúvida "qual lado é o chão"** nasce quando há pistas contraditórias: móveis de um jeito, gravidade de outro. Repetir o mesmo móvel em orientações diferentes reforça isso. |
| **Superliminal** (Pillow Castle, 2019) | Unity (confirmado) | Ilusões de perspectiva e escala; o espaço engana o jogador. | Um detalhe "errado" bem posicionado vale mais do que muitos. A **corrente esticada para cima** é o nosso detalhe "errado" principal. |
| **Layers of Fear** (Bloober Team, 2016) | Unity (confirmado; o remake de 2023 é Unreal 5) | Mansão vitoriana em que salas se transformam quando o jogador vira as costas: quadros, móveis e portas mudam de lugar e de orientação. | A estética que buscamos: madeira escura, quadros e velas. Transformações sutis (pêndulo para cima, fogo para baixo) são mais assustadoras que as óbvias. |
| **Rooms: The Unsolvable Puzzle** (HandMade Game) | Unity (confirmado) | Puzzle em VR com salas que deslizam e mudam de lugar. | Em VR, **o puzzle precisa ser legível pela forma**: a corrente sair do guincho deixa claro onde usar a manivela. |
| **Wayward Sky** (Uber Entertainment, PSVR) | Unity (confirmado) | Puzzles em VR com mudança de nível e altura. | Quedas e alturas em VR funcionam melhor **vistas de fora** (um objeto caindo), não com a câmera do jogador caindo. É o caso do lustre. |
| **The Last Clockwinder** (Pontoco, 2022) | Unity (confirmado) | Puzzle em VR premiado. | Um ponto de interação claro por puzzle; feedback forte (som e movimento) ao resolver. |
| **The Room VR: A Dark Matter** (Fireproof) | não confirmado | Escape room em VR com mecanismos (manivelas, engrenagens), objetos para examinar e combinar. | O "clique" mecânico (guincho, catraca, engrenagem) é o que dá prazer ao resolver. Vale caprichar no **som e no giro** da manivela. |
| **Hotel Infinity** | não confirmado | Corredores e salas impossíveis em VR com pouco espaço físico. | Espaços impossíveis em VR não precisam de locomoção extra; a ilusão está no cenário. |
| **traVRsal** | SDK em Unity | Salas que se sobrepõem no espaço físico. | (Conceitual) mostra que espaço impossível é um tema forte e popular em VR. |

## O que aplicar no Salão Invertido v2
1. **A corrente sai do chão e sobe esticada** até o lustre lá no alto. É o elemento "impossível" central e planta a dúvida "quem está de ponta-cabeça: eu ou a sala?".
2. **Sala bem mais alta (~9 m)**: o lustre fica a uns 6 m do chão e a queda dura cerca de 1 s. É dramático, mas visto de fora (confortável em VR).
3. **Pistas contraditórias**, não todas de ponta-cabeça:
   - fogo para baixo, pêndulo para cima e retratos invertidos;
   - a corrente esticada para cima, como se a gravidade puxasse para lá;
   - a hera "crescendo para baixo" a partir do assoalho lá em cima.
4. **Um ponto de interação por puzzle**: o guincho no chão, com a manivela, a corrente e o lustre alinhados na vertical.
5. **Feedback forte ao resolver**: catraca da manivela, corrente afrouxando, queda, estrondo de vidro e destroços com o Livro brilhando.
6. **Conforto em VR**: o jogador olha para cima com frequência, então os elementos importantes (guincho, destroços, porta) ficam no nível dos olhos ou do chão.

## Fontes
- [Manifold Garden (Wikipedia)](https://en.wikipedia.org/wiki/Manifold_Garden) · [Steam](https://store.steampowered.com/app/473950/Manifold_Garden/)
- [Superliminal (Wikipedia)](https://en.wikipedia.org/wiki/Superliminal) · [Steam](https://store.steampowered.com/app/1049410/Superliminal/)
- [Layers of Fear (Wikipedia)](https://en.wikipedia.org/wiki/Layers_of_Fear) · [Remake em Unreal Engine 5](https://www.unrealengine.com/en-US/developer-interviews/layers-of-fear-reimagines-horror-with-unreal-engine-5)
- [Rooms: The Unsolvable Puzzle (ModDB)](https://www.moddb.com/games/rooms-the-unsolvable-puzzle)
- [Wayward Sky (Wikipedia)](https://en.wikipedia.org/wiki/Wayward_Sky)
- [The Last Clockwinder (Wikipedia)](https://en.wikipedia.org/wiki/The_Last_Clockwinder)
- [The Room VR: A Dark Matter (UploadVR)](https://www.uploadvr.com/the-room-a-dark-matter-quest-preview/)
- [Hotel Infinity (VR Today)](https://vrtodaymagazine.com/hotel-infinity-immersive-vr-adventure/)
- [traVRsal](https://www.travrsal.com/)
- [Developing an Escape Room-Inspired VR Game in Unity (80.lv)](https://80.lv/articles/developing-an-escape-room-inspired-vr-game-in-unity)
