# Portal: pontos de atenção para VR (Cardboard)

Caio, revisei o `Assets/Mechanics/Portal` (commit 2c7722d). A mecânica está boa para desktop, mas tem riscos para a versão Cardboard. Seguem os pontos, do mais importante para o menos.

## 1. Não renderiza em estéreo (crítico no Cardboard)

`Portal.cs` usa **uma** câmera de portal e **uma** `viewTexture`, com a matriz da câmera principal:

- `Portal.cs:90` e `:271-273` → `portalCam.projectionMatrix = playerCam.projectionMatrix` / `CalculateObliqueMatrix(...)`
- `Portal.cs:196` → uma única `targetTexture`

No Cardboard, cada olho tem posição e projeção próprias. Com uma textura só, os dois olhos veem a mesma imagem dentro da porta: o portal parece um "pôster" chapado, sem profundidade, e pode causar desconforto.

**Opções:**

| Opção | Como | Custo |
|---|---|---|
| A. Render por olho | Duas render textures; posicionar a `portalCam` com `playerCam.GetStereoViewMatrix(eye)` / `GetStereoProjectionMatrix(eye)` e o shader escolhe a textura por `unity_StereoEyeIndex` | Dobra o custo do portal (2 renders extras por portal visível) |
| B. Portal por stencil | As duas salas existem na cena (já carregamos aditivamente); a porta escreve no stencil e a sala de destino só desenha onde o stencil marca | Barato e funciona em estéreo naturalmente, mas as salas precisam estar fisicamente "atrás" da porta (sem sobreposição) |
| C. Sem portal visível no VR | No VR a porta vira transição (fade + teleporte); o portal renderizado fica só no PC | Mais simples; perde o efeito no celular |

## 2. Desempenho no celular

Cada portal visível renderiza a cena de novo (e com recursão, mais vezes). No celular, com Cardboard (já são 2 renders por frame), isso pesa. Hoje `recursionLimit = 5` por padrão (`Portal.cs:11`), ou seja, até 5 renders extras por portal; vale baixar para 1. O código já pula portais fora da visão (`Portal.cs:79`, `CameraUtility.VisibleFromCamera`), mas testa com a câmera central, não com cada olho.

## 3. `UniversalRenderPipeline.RenderSingleCamera` está obsoleto

`Portal.cs:115`: no URP 17 (Unity 6) a API recomendada é `RenderPipeline.SubmitRenderRequest`. Hoje deve gerar só um aviso, mas pode virar erro numa atualização.

## 4. Teleporte do player: já corrigido

O player usa `CharacterController`, que desfaz mudanças diretas em `transform.position`. Isso fazia o `Teleport` padrão do `PortalTraveller` não ter efeito, ou ter efeito com "tranco". Na branch `feature/sistemas-base` adicionei um `override Teleport` em `PlayerInputLogic`, que:

- desliga o controller durante o teleporte;
- aplica só o giro horizontal (yaw) e mantém a inclinação da câmera;
- converte a velocidade (pulo/queda) para a orientação do portal de saída.

## 5. Detalhes menores

- `Portal.cs:26` usa `Camera.main` no `Awake`. Se a câmera mudar (ou ainda não existir quando a sala carrega de forma aditiva), o portal fica sem referência.
- `MainCamera.cs` pega os portais com `FindObjectsByType` só no `Awake`. Portais de salas carregadas depois (`ScenePartLoader`) não entram na lista. Sugestão: cada `Portal` se registrar numa lista estática no `OnEnable`/`OnDisable`.
- `PlayerInputLogic` precisa ter `graphicsObject` definido no Inspector (o `PortalTraveller` usa no `EnterPortalThreshold`); se ficar vazio, dá erro ao encostar no portal.
