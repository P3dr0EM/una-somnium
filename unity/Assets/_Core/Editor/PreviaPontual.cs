using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>Prévias pontuais para conferir modelos (batch). Abre a cena e fotografa um objeto pelo nome.</summary>
    public static class PreviaPontual
    {
        static void Shot(string dir, string scene, string obj, string name, Vector3 offset, string activate = null)
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            if (activate != null)
                foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                    if (t.name == activate) t.gameObject.SetActive(true);
            var go = GameObject.Find(obj);
            if (go == null) { Debug.LogWarning($"[Somnium] Não achei {obj}"); return; }
            var b = RoomKit.WorldBounds(go);
            float d = Mathf.Max(0.6f, b.size.magnitude * 1.2f);
            using (var s = new RoomKit.Shooter(dir, "ponto", b.center + offset.normalized * d, false))
                s.Shot(name, b.center + offset.normalized * d, b.center);
        }

        public static void Run()
        {
            string dir = RoomKit.PreviewDir();
            Shot(dir, ColinasBuilder.ScenePath, "ColinasXP/Pickup_Manivela", "manivela", new Vector3(0.6f, 0.8f, -1f));
            Shot(dir, SaguaoBuilder.ScenePath, "Saguao/Estatua", "estatua_saguao", new Vector3(0.2f, 0.3f, -1f));
            Shot(dir, SalaoBuilder.ScenePath, "SalaoInvertido/Lareira_Invertida", "lareira", new Vector3(0.2f, -0.6f, -1f));
            Shot(dir, EstufaBuilder.ScenePath, "Estufa/Irrigacao/Canos", "sprinklers", new Vector3(0.3f, -0.5f, -1f));
            Shot(dir, SaguaoBuilder.ScenePath, "Saguao/Estatua/MedalhaoEncaixado", "medalhao_encaixado", new Vector3(0.15f, 0.1f, -1f), "MedalhaoEncaixado");
            Shot(dir, SalaoBuilder.ScenePath, "SalaoInvertido/Lustre_Puzzle/Lustre", "lustre_mesa", new Vector3(0.8f, -1f, -0.6f));
            Shot(dir, EscritorioBuilder.ScenePath, "EscritorioAzul/Alcapao/Corrente/Cadeado", "cadeado", new Vector3(0.2f, 0.6f, -1f));
            Shot(dir, EscritorioBuilder.ScenePath, "EscritorioAzul/Alcapao/Nicho/Pickup_LataRacao", "lata", new Vector3(0.3f, 0.9f, -1f), "Nicho");
        }
    }
}
