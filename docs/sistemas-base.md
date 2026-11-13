# Sistemas base: como usar

Referência técnica dos sistemas da branch `feature/sistemas-base`. Para quem monta salas e puzzles no Unity.

## Visão geral

| Sistema | Onde | O que faz |
|---|---|---|
| Input | `_Core/Scripts/GameInput.cs` | Lê as ações do `InputSystem_Actions` (project-wide). Sabe se o jogador está no gamepad ou no teclado (para mostrar "A" ou "E"). |
| Estado | `_Core/Scripts/GameState.cs` | `GameState` (menu aberto, VR ativo) e `WorldState` (o que já foi pego/destrancado/examinado). |
| HUD | `_Core/Scripts/UI/HUD.cs` | Mira, dica "[E] Abrir" e mensagens "Obteve: X". Em world space (funciona no Cardboard). |
| Player | `Scripts/PlayerInputLogic.cs` | Movimento e câmera (PC e VR), teleporte de portal. Adiciona sozinho a mira e o inventário. |
| Mira | `Mechanics/Interaction/Scripts/PlayerInteractor.cs` | Raio do centro da câmera; paredes bloqueiam; chama `Interact()`. |
| Trava | `Mechanics/Interaction/Scripts/ItemLock.cs` | Objeto que precisa de item(ns). |
| Itens | `Mechanics/Inventory/Scripts/` | `ItemData` (asset), `ItemPickup` (no cenário), `Inventory`, `InventoryUI`. |
| Inspeção | `Mechanics/Inspection/Scripts/` | `ItemInspector` (item 3D no inventário) e `InspectHotspot` (segredos). |
| Cardboard | `_Core/Scripts/Platform/CardboardBootstrap.cs` | Tela acesa, QR do óculos, recentrar. Só roda com o Cardboard ativo. |
| Spawn | `_Core/Scripts/PlayerSpawnPoint.cs` | Leva o jogador até o ponto no Start (cenas de teste). |
| Menu | `_Core/Editor/SomniumMenu.cs` | Menu **Somnium** no editor. |

## Receitas

### Criar um item
1. **Somnium → Itens → Criar itens do roteiro** cria os 14 itens do roteiro em `Assets/Data/Items` (não sobrescreve os existentes).
2. Item novo: botão direito em `Assets/Data/Items` → **Create → Somnium → Item**.
3. Campos:
   - **Display Name** e **Description**: o texto mostrado no inventário.
   - **Icon**: opcional.
   - **Inspect Model**: o modelo 3D mostrado no inventário. Use um FBX ou prefab simples; scripts e colliders são removidos na exibição. Sem modelo, aparece um cubo.
   - **Inspect Size**: a maior dimensão do item no inventário, em metros.
   - **Combinations**: com qual item combina e o que resulta.

### Colocar um item no cenário
1. Objeto com **Collider** (não precisa de layer especial).
2. Adicione **Item Pickup** e arraste o `ItemData` para **Item**.
3. Opcional: **Pickup Message** e **Save Id**. O Save Id é necessário se houver dois objetos com o mesmo nome no mesmo caminho da hierarquia.

### Trava que precisa de item (alicate na corrente, manivela no mecanismo, ...)
1. Objeto com **Collider** + **Item Lock**.
2. **Required Items**: os itens necessários. **Consume Items**: se eles saem do inventário.
3. **Accept Partial**: o jogador vai inserindo aos poucos (porta principal com 5 chaves, mostra "3/5").
4. **Locked Message**: dica quando falta o item. **Prompt Text** (no topo): texto da mira, ex.: "Examinar corrente". Quando o jogador tem o item, a mira mostra "Usar <item>" sozinha.
5. Resultado:
   - **Reward Item**: item recebido, ex.: galão + cartucho dá Herbicida.
   - **Activate/Deactivate On Unlock**: ligar/desligar objetos, ex.: esconder a corrente ou mostrar a chave.
   - **On Unlocked**: evento para animações, ex.: derrubar o lustre.
   - **On Restored Unlocked**: chamado se a sala for recarregada já destravada. Use para pôr o estado final sem animação.

### Segredo na inspeção (livro falso)
1. No prefab do modelo de inspeção, crie um filho no ponto do segredo e adicione **Inspect Hotspot**.
2. **Local Facing**: o lado que precisa ficar virado para o jogador (o gizmo amarelo mostra a direção).
3. **Prompt**, **Message**, **Reward Item** (ex.: Alicate) e, opcional, **Transforms Into** (ex.: Livro Antigo vira Livro Aberto).
4. Exemplo pronto: `Assets/Rooms/_Teste/Livro_Inspecao.prefab`.

### Objeto interagível próprio (código)
Herde de `Assets.Scripts.Interactable`:

```csharp
public class MeuMecanismo : Interactable
{
    public override string Prompt => "Puxar alavanca";   // texto da mira (opcional)
    public override void Interact() { /* ... */ }
}
```

Para lembrar o estado entre recargas da sala, use `WorldState.Has/Set` com `WorldState.KeyFor(this, "prefixo", saveId)`.

## Testar
**Somnium → Teste → Abrir cena de teste** e Play. Roteiro completo em `docs/guias/teste-sistemas-base.md`.

## Detalhes técnicos
- **Por que world space**: no Cardboard, UI colada na tela (Screen Space Overlay) não aparece em estéreo.
- **Inventário aberto**: `Time.timeScale = 0` e a câmera passa a mostrar só a layer **UI**, com fundo escuro, para paredes próximas não cobrirem o item. Tudo volta ao fechar.
- **Domain reload desligado**: o projeto usa "Enter Play Mode Options". Os estáticos dos sistemas são zerados com `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`.
- **Input**: as ações novas (`Inventory`, `Combine`) estão no `InputSystem_Actions.inputactions`. O código lê por nome (`InputSystem.actions.FindAction("Player/Inventory")`), sem depender da classe gerada.
- **Mira**: raio com alcance de 2,5 m em todas as layers. Volumes trigger sem Interactable são ignorados, então o `ScenePartLoader` não bloqueia.

## Adicionados em 2026-11-12

| Componente | Onde | Uso |
|---|---|---|
| `MessageInteractable` | `Mechanics/Interaction/Scripts` | Placas, bilhetes, portas trancadas: mostra **Message** ao interagir. **Prompt Text** = texto da mira (padrão "Examinar"). |
| `LockProgressIndicator` | `Mechanics/Interaction/Scripts` | Mostra o progresso de um `ItemLock` com Accept Partial: liga os N primeiros de **Inserted Visuals** (ex.: chaves nas fechaduras). |
| `ItemLock.onProgressChanged(int)` | | Evento com quantos itens já foram inseridos (também disparado no Start). |
| `Itr_Door.interactableByPlayer` | `Scripts/Itr_Door.cs` | Desligado = a mira ignora a porta; ela só abre por evento (ex.: `GameEnding`). |
| `ScreenFader` | `_Core/Scripts/UI` | `yield return ScreenFader.Instance.FadeTo(cor, alpha, segundos)` e `ShowText(...)`. Cobre o campo de visão também no Cardboard. |
| `GameEnding` | `_Core/Scripts/Flow` | Final: abre **Doors To Open**, liga **Activate On Begin**, fade branco, **Message**, e recomeça (`GameEnding.Restart()` limpa o inventário e o WorldState e recarrega as cenas abertas, que precisam estar na lista de cenas do build). |
| `RoomSettings` | `_Core/Scripts/Flow` | Um por sala: torna a cena da sala a ativa ao carregar, para valer a luz ambiente e a neblina dela. |

### Gerar uma sala por script (modelo: Saguão)
- `Rooms/00_Saguao/Editor/SaguaoBuilder.cs` monta a cena com blocos (cubos com materiais URP em `Rooms/00_Saguao/Materials`), liga os puzzles e salva.
- **Gerar de novo sobrescreve a cena** (o menu pede confirmação). Depois de editar à mão, não gere de novo.
- `RenderPreviews()` salva PNGs de vistas fixas (padrão `unity/Temp/Previews`). Em batch: `-executeMethod Somnium.EditorTools.Rooms.SaguaoBuilder.RenderPreviews -previewOut <pasta>` (sem `-nographics`).

### Menu de debug (só com o jogo rodando)
- **Somnium → Debug → Receber chaves 1-2-3-5 e o medalhão** (a 4 vem da estátua).
- **Receber todos os itens do roteiro** e **Limpar inventário e estado do mundo**.
