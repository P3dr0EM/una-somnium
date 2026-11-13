using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnium.Core
{
    /// <summary>
    /// Coloque um em cada cena de sala. Ao carregar, torna esta cena a "ativa", para que a iluminação
    /// ambiente, a neblina e o skybox definidos nela (Window > Rendering > Lighting) passem a valer,
    /// e não os da cena "persistencia".
    /// </summary>
    public class RoomSettings : MonoBehaviour
    {
        [Tooltip("Nome mostrado ao entrar (futuro). Por enquanto só informativo.")]
        public string roomName = "";

        void Start()
        {
            if (gameObject.scene.isLoaded) SceneManager.SetActiveScene(gameObject.scene);
        }
    }
}
