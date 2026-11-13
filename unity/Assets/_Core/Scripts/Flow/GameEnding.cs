using System.Collections;
using System.Collections.Generic;
using Somnium.Items;
using Somnium.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnium.Core
{
    /// <summary>
    /// Final do jogo (Porta Principal aberta): abre as portas, liga a luz de fora, clareia a tela até o branco,
    /// mostra a mensagem e recomeça (limpa inventário e estado do mundo e recarrega as cenas abertas).
    /// Ligue Begin() no evento "On Unlocked" do ItemLock da porta.
    /// </summary>
    public class GameEnding : MonoBehaviour
    {
        public Itr_Door[] doorsToOpen = new Itr_Door[0];
        [Tooltip("Objetos ligados no início do final (ex.: a luz que entra pela porta).")]
        public GameObject[] activateOnBegin = new GameObject[0];

        [TextArea] public string message = "Você acordou.";
        public float delayBeforeFade = 1.2f;
        public float fadeSeconds = 3f;
        public float messageSeconds = 4f;
        public bool restartAfter = true;

        bool running;

        public void Begin()
        {
            if (running) return;
            running = true;
            StartCoroutine(Sequence());
        }

        IEnumerator Sequence()
        {
            GameState.InMenu = true; // trava movimento e interação
            foreach (var go in activateOnBegin) if (go != null) go.SetActive(true);
            foreach (var door in doorsToOpen) if (door != null && !door.isOpen) door.Interact();

            yield return new WaitForSecondsRealtime(delayBeforeFade);

            var fader = ScreenFader.Instance;
            yield return fader.FadeTo(Color.white, 1f, fadeSeconds);
            yield return fader.ShowText(message, new Color(0.15f, 0.15f, 0.18f), 1.5f);
            yield return new WaitForSecondsRealtime(messageSeconds);

            if (!restartAfter) yield break;
            Restart();
        }

        /// <summary>Recomeça do zero: esvazia inventário e WorldState e recarrega as cenas que estavam abertas.</summary>
        public static void Restart()
        {
            var paths = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded && !string.IsNullOrEmpty(s.path)) paths.Add(s.path);
            }

            WorldState.Clear();
            Inventory.Instance.Clear();
            GameState.InMenu = false;
            Time.timeScale = 1f;

            SceneReloader.Reload(paths);
        }

        /// <summary>Recarrega uma lista de cenas: a primeira sozinha, as outras aditivamente (se ainda não estiverem carregando).</summary>
        class SceneReloader : MonoBehaviour
        {
            List<string> pending;

            public static void Reload(List<string> paths)
            {
                if (paths.Count == 0) return;
                var go = new GameObject("[SceneReloader]");
                DontDestroyOnLoad(go);
                var r = go.AddComponent<SceneReloader>();
                r.pending = paths;
                r.StartCoroutine(r.Run());
            }

            IEnumerator Run()
            {
                yield return SceneManager.LoadSceneAsync(pending[0], LoadSceneMode.Single);
                // Espera os Start() da cena principal (ex.: LevelPersistencia já pede as cenas dela).
                yield return null;
                yield return null;

                for (int i = 1; i < pending.Count; i++)
                {
                    if (SceneManager.GetSceneByPath(pending[i]).IsValid()) continue;
                    var op = SceneManager.LoadSceneAsync(pending[i], LoadSceneMode.Additive);
                    if (op == null) Debug.LogWarning($"[GameEnding] Não consegui recarregar '{pending[i]}'. Ela está em File > Build Profiles > Scene List?");
                    else yield return op;
                }
                Destroy(gameObject);
            }
        }
    }
}
