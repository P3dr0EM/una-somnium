using Assets.Scripts;
using Somnium.Core;
using Somnium.UI;
using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Mira do jogador: a cada frame lança um raio do centro da câmera, encontra o Interactable
    /// na frente, mostra a dica no HUD e chama Interact() ao apertar o botão de interagir.
    /// Paredes bloqueiam o raio (não dá para interagir através delas).
    /// Adicionado automaticamente pelo PlayerInputLogic.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("Alcance da mão, em metros.")]
        public float range = 2.5f;

        [Tooltip("Layers que o raio enxerga. Paredes devem estar incluídas para bloquear a mira.")]
        public LayerMask mask = Physics.DefaultRaycastLayers;

        [SerializeField] Camera viewCamera;

        readonly RaycastHit[] hits = new RaycastHit[16];
        Interactable focused;

        public Interactable Focused => focused;

        public void Setup(Camera cam) => viewCamera = cam;

        void Update()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            var hud = HUD.EnsureFor(viewCamera);

            if (GameState.InMenu || GameState.MenuClosedThisFrame)
            {
                focused = null;
                hud.SetPrompt(null);
                hud.SetFocused(false);
                return;
            }

            focused = FindTarget();
            if (focused == null)
            {
                hud.SetPrompt(null);
                hud.SetFocused(false);
                return;
            }

            hud.SetPrompt($"[{GameInput.InteractLabel}] {focused.Prompt}");
            hud.SetFocused(true);

            if (GameInput.InteractPressed) focused.Interact();
        }

        Interactable FindTarget()
        {
            var origin = viewCamera.transform.position;
            var dir = viewCamera.transform.forward;
            int count = Physics.RaycastNonAlloc(origin, dir, hits, range, mask, QueryTriggerInteraction.Collide);

            // O acerto mais próximo decide: se for parede, bloqueia; se for interagível, é o alvo.
            float best = float.MaxValue;
            Interactable result = null;
            for (int i = 0; i < count; i++)
            {
                var col = hits[i].collider;
                if (col.transform.IsChildOf(transform)) continue; // o próprio corpo do jogador

                var candidate = col.GetComponentInParent<Interactable>();
                if (col.isTrigger && candidate == null) continue; // volumes de gatilho (ScenePartLoader etc.)

                if (hits[i].distance < best)
                {
                    best = hits[i].distance;
                    result = candidate;
                }
            }
            return result != null && result.CanInteract ? result : null;
        }
    }
}
