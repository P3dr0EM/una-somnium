using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnium.Core
{
    /// <summary>
    /// Ponto onde o jogador aparece (posição + direção do eixo azul).
    /// - "Place On Start": ao abrir a cena direto (teste no editor), leva o jogador até aqui. Só um por cena.
    /// - "Id": usado pelas portas entre salas (RoomTransition) para escolher onde o jogador chega.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Nome deste ponto, usado pelas portas (ex.: 'inicio', 'de_colinas').")]
        public string id = "inicio";

        [Tooltip("Ao abrir a cena direto, levar o jogador até aqui. Deixe ligado em só um ponto por cena.")]
        public bool placeOnStart = true;

        void Start()
        {
            // Chegando por uma porta, quem posiciona é o RoomTransition.
            if (!placeOnStart || RoomTransition.InProgress) return;
            PlacePlayer();
        }

        public void PlacePlayer()
        {
            var player = FindAnyObjectByType<PlayerInputLogic>();
            if (player == null)
            {
                Debug.LogWarning("[PlayerSpawnPoint] Nenhum Player na cena. Abra a 'persistencia' junto com esta cena.", this);
                return;
            }
            // O ponto fica no chão; sobe o jogador para a cápsula não nascer enterrada.
            var cc = player.playerController;
            // Distância do pivô do jogador até o pé da cápsula (em mundo, já com a escala do objeto).
            float lift = cc != null ? player.transform.position.y - cc.bounds.min.y + 0.05f : 1f;
            player.PlaceAt(transform.position + Vector3.up * lift, transform.eulerAngles.y);
        }

        /// <summary>Ponto com o id pedido dentro da cena; sem id igual, o primeiro "Place On Start" da cena.</summary>
        public static PlayerSpawnPoint Find(Scene scene, string id)
        {
            PlayerSpawnPoint fallback = null;
            foreach (var p in FindObjectsByType<PlayerSpawnPoint>())
            {
                if (p.gameObject.scene != scene) continue;
                if (p.id == id) return p;
                if (fallback == null && p.placeOnStart) fallback = p;
            }
            return fallback;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = placeOnStart ? Color.cyan : new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + transform.forward);
        }
    }
}
