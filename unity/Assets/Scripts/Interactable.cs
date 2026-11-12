using UnityEngine;

namespace Assets.Scripts
{
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Texto da dica ao mirar (ex.: 'Examinar corrente'). Vazio = texto padrão do tipo de objeto.")]
        [SerializeField] protected string promptText = "";

        /// <summary>Texto mostrado na mira: "[E] " + Prompt.</summary>
        public virtual string Prompt => string.IsNullOrEmpty(promptText) ? "Interagir" : promptText;

        /// <summary>Se false, a mira ignora este objeto.</summary>
        public virtual bool CanInteract => isActiveAndEnabled;

        public abstract void Interact();
    }
}