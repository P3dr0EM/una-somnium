using Assets.Scripts;
using Somnium.Core;
using Somnium.UI;
using UnityEngine;

namespace Somnium.Items
{
    /// <summary>
    /// Item no cenário que vai para o inventário ao interagir. Precisa de um Collider (não-trigger de preferência).
    /// Depois de pego, continua sumido mesmo se a sala for descarregada e recarregada.
    /// </summary>
    public class ItemPickup : Interactable
    {
        public ItemData item;
        [Tooltip("Mensagem ao pegar. Vazio = 'Obteve: <item>'.")]
        [TextArea] public string pickupMessage;
        [Tooltip("Id único para o save. Vazio = cena + caminho na hierarquia.")]
        public string saveId;

        string Key => WorldState.KeyFor(this, "pickup", saveId);

        public override string Prompt =>
            !string.IsNullOrEmpty(promptText) ? promptText : (item != null ? $"Pegar {item.DisplayName}" : "Pegar");

        void Start()
        {
            if (WorldState.Has(Key)) gameObject.SetActive(false);
        }

        public override void Interact()
        {
            if (item == null)
            {
                Debug.LogWarning($"[ItemPickup] '{name}' está sem ItemData.", this);
                return;
            }

            Inventory.Instance.Add(item);
            WorldState.Set(Key);
            HUD.Instance.ShowMessage(string.IsNullOrEmpty(pickupMessage) ? $"Obteve: {item.DisplayName}" : pickupMessage);
            gameObject.SetActive(false);
        }
    }
}
