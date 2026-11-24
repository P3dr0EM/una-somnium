using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnium.Core
{
    /// <summary>
    /// Só para testes (não faz nada sem os argumentos). Abre o executável com:
    ///   Somnium.exe -somniumIrPara Assets/Rooms/02_EscritorioAzul/escritorio_azul.unity -somniumCaptura C:\pasta\print.png
    /// Espera o Saguão carregar, troca de sala como uma porta faria, tira um print, registra onde o jogador está e fecha.
    /// </summary>
    public class TesteAutomatico : MonoBehaviour
    {
        string target, capture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string t = Arg("-somniumIrPara"), c = Arg("-somniumCaptura");
            if (t == null && c == null) return;
            var go = new GameObject("[TesteAutomatico]");
            DontDestroyOnLoad(go);
            var me = go.AddComponent<TesteAutomatico>();
            me.target = t;
            me.capture = c;
        }

        static string Arg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(4f);
            if (!string.IsNullOrEmpty(target))
            {
                var from = SceneManager.GetSceneByName("saguao");
                RoomTransition.Go(target, "inicio", from);
                yield return new WaitForSeconds(4f);
            }
            // -somniumDesligar Nome1,Nome2: desliga objetos com esses nomes na cena ativa (para isolar problemas).
            string off = Arg("-somniumDesligar");
            if (!string.IsNullOrEmpty(off))
                foreach (var name in off.Split(','))
                    foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
                        if (t != null && t.name == name && t.gameObject.scene == SceneManager.GetActiveScene())
                        {
                            t.gameObject.SetActive(false);
                            Debug.Log($"[Teste] Desligado: {name}");
                        }
            yield return null;
            // -somniumMedir Nome1,Nome2: registra tamanho e escala reais desses objetos.
            string measure = Arg("-somniumMedir");
            if (!string.IsNullOrEmpty(measure))
                foreach (var name in measure.Split(','))
                    foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                        if (t != null && t.name == name)
                        {
                            var rs = t.GetComponentsInChildren<Renderer>(true);
                            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(t.position, Vector3.zero);
                            foreach (var r in rs) b.Encapsulate(r.bounds);
                            Debug.Log($"[Teste] {t.name}: pos {t.position} escala {t.lossyScale} tamanho {b.size} renderers {rs.Length}");
                            foreach (var r in rs)
                            {
                                var mf = r.GetComponent<MeshFilter>();
                                Debug.Log($"[Teste]   {r.name}: escala {r.transform.lossyScale} malha {(mf && mf.sharedMesh ? mf.sharedMesh.name + " " + mf.sharedMesh.bounds.size : "-")} bounds {r.bounds.size}");
                            }
                        }
            int shadowLights = 0;
            foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude))
                if (l.shadows != LightShadows.None) { shadowLights++; Debug.Log($"[Teste] Luz com sombra: {l.name} ({l.type}) em {l.transform.parent?.name}"); }
            var cam = Camera.main;
            Debug.Log($"[Teste] Cena ativa: {SceneManager.GetActiveScene().name}; câmera: {(cam ? cam.transform.position.ToString() : "nenhuma")}; " +
                      $"cenas: {SceneManager.sceneCount}; fader/menu: {GameState.InMenu}");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Debug.Log($"[Teste] Cena {i}: {SceneManager.GetSceneAt(i).name} carregada={SceneManager.GetSceneAt(i).isLoaded}");
            if (!string.IsNullOrEmpty(capture)) ScreenCapture.CaptureScreenshot(capture);
            yield return new WaitForSeconds(2f);
            Application.Quit();
        }
    }
}
