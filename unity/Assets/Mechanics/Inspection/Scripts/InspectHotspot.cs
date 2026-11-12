using Somnium.Items;
using UnityEngine;

namespace Somnium.Inspection
{
    /// <summary>
    /// Ponto escondido num modelo de inspeção (ex.: trava na lombada do Livro Antigo).
    /// Quando o jogador gira o item até este ponto ficar virado para ele, aparece a dica
    /// e o botão de interagir ativa o segredo.
    /// Coloque como filho do modelo usado em ItemData.inspectModel e aponte o eixo azul (Z) para fora da superfície.
    /// </summary>
    public class InspectHotspot : MonoBehaviour
    {
        [Tooltip("Id único dentro deste item (salva que o segredo já foi encontrado).")]
        public string id = "segredo";
        [Tooltip("Direção local que precisa apontar para o jogador.")]
        public Vector3 localFacing = Vector3.forward;
        [Range(0.5f, 1f)]
        [Tooltip("Quão de frente precisa estar (1 = exatamente de frente).")]
        public float facingThreshold = 0.85f;

        public string prompt = "Examinar";
        [TextArea] public string message;

        [Tooltip("Item recebido ao ativar (ex.: Alicate).")]
        public ItemData rewardItem;
        [Tooltip("O item inspecionado vira este (ex.: Livro Antigo -> Livro Aberto). Vazio = não muda.")]
        public ItemData transformsInto;

        public bool IsFacing(Vector3 viewerPosition)
        {
            Vector3 normal = transform.TransformDirection(localFacing).normalized;
            Vector3 toViewer = (viewerPosition - transform.position).normalized;
            return Vector3.Dot(normal, toViewer) >= facingThreshold;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 n = transform.TransformDirection(localFacing).normalized;
            Gizmos.DrawLine(transform.position, transform.position + n * 0.1f);
            Gizmos.DrawWireSphere(transform.position, 0.01f);
        }
    }
}
