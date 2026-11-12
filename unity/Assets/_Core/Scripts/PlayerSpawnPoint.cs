using UnityEngine;

namespace Somnium.Core
{
    /// <summary>
    /// Ao começar, leva o jogador até este ponto (posição + direção do eixo azul).
    /// Usado em cenas de teste carregadas junto com a "persistencia" (onde o Player mora).
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        void Start()
        {
            var player = FindAnyObjectByType<PlayerInputLogic>();
            if (player == null)
            {
                Debug.LogWarning("[PlayerSpawnPoint] Nenhum Player na cena. Abra a 'persistencia' junto com esta cena.", this);
                return;
            }
            // O ponto fica no chão; sobe o jogador para a cápsula não nascer enterrada.
            var cc = player.playerController;
            float lift = cc != null ? cc.height * 0.5f - cc.center.y + cc.skinWidth : 1f;
            player.PlaceAt(transform.position + Vector3.up * lift, transform.eulerAngles.y);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward);
        }
    }
}
