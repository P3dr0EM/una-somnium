using Assets.Scripts;
using Somnium.UI;
using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Objeto que só mostra uma mensagem ao interagir: placas, bilhetes, portas trancadas,
    /// o clássico "Examinar" do survival horror.
    /// </summary>
    public class MessageInteractable : Interactable
    {
        [TextArea(2, 6)] public string message = "Nada de especial.";
        [Tooltip("Segundos na tela.")]
        public float duration = 3.5f;

        public override string Prompt => string.IsNullOrEmpty(promptText) ? "Examinar" : promptText;

        public override void Interact() => HUD.Instance.ShowMessage(message, duration);
    }
}
