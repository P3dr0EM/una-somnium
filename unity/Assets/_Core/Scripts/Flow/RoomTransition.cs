using System.Collections;
using Somnium.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnium.Core
{
    /// <summary>
    /// Troca de sala: escurece a tela, carrega a cena da sala de destino (aditiva, ao lado da "persistencia"),
    /// põe o jogador no PlayerSpawnPoint com o id pedido, descarrega a sala de origem e clareia.
    /// O estado das salas (itens pegos, travas abertas) fica no WorldState, então voltar a uma sala a mantém como estava.
    /// </summary>
    public static class RoomTransition
    {
        /// <summary>True durante uma troca. O PlayerSpawnPoint "inicial" da sala nova não age nesse caso.</summary>
        public static bool InProgress { get; private set; }

        public const float FadeSeconds = 0.35f;

        /// <summary>Vai para a cena <paramref name="scenePath"/> (caminho em Assets/..., precisa estar na lista de cenas do build).</summary>
        public static void Go(string scenePath, string spawnId, Scene fromScene)
        {
            if (InProgress || string.IsNullOrEmpty(scenePath)) return;
            InProgress = true;
            var runner = new GameObject("[RoomTransition]").AddComponent<Runner>();
            Object.DontDestroyOnLoad(runner.gameObject);
            runner.StartCoroutine(runner.Run(scenePath, spawnId, fromScene));
        }

        class Runner : MonoBehaviour
        {
            public IEnumerator Run(string scenePath, string spawnId, Scene fromScene)
            {
                GameState.InMenu = true;
                var fader = ScreenFader.Instance;
                yield return fader.FadeTo(Color.black, 1f, FadeSeconds);

                var target = SceneManager.GetSceneByPath(scenePath);
                if (!target.isLoaded)
                {
                    var op = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                    if (op == null)
                    {
                        Debug.LogError($"[RoomTransition] Não consegui carregar '{scenePath}'. Ela está em File > Build Profiles > Scene List?");
                        yield return Finish(fader);
                        yield break;
                    }
                    yield return op;
                    target = SceneManager.GetSceneByPath(scenePath);
                }
                SceneManager.SetActiveScene(target);

                // Espera um frame para os Start() da sala nova rodarem (estado restaurado, etc.).
                yield return null;

                var spawn = PlayerSpawnPoint.Find(target, spawnId);
                if (spawn != null) spawn.PlacePlayer();
                else Debug.LogWarning($"[RoomTransition] Nenhum PlayerSpawnPoint '{spawnId}' em {target.name}.");

                if (fromScene.IsValid() && fromScene.isLoaded && fromScene != target)
                    yield return SceneManager.UnloadSceneAsync(fromScene);

                yield return Finish(fader);
            }

            IEnumerator Finish(ScreenFader fader)
            {
                yield return fader.FadeTo(Color.black, 0f, FadeSeconds);
                GameState.InMenu = false;
                GameState.MarkMenuClosed();
                InProgress = false;
                Destroy(gameObject);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => InProgress = false;
    }
}
