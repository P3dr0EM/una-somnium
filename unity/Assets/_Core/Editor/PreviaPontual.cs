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

        /// <summary>Acha o objeto pelo nome (mesmo desligado), liga ele e os pais, esconde <paramref name="hide"/> e fotografa.</summary>
        static void ShotNamed(string dir, string scene, string obj, string name, Vector3 offset, string hide = null)
        {
            EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            GameObject go = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == obj && go == null) go = t.gameObject;
                if (hide != null && t.name == hide) t.gameObject.SetActive(false);
            }
            if (go == null) { Debug.LogWarning($"[Somnium] Não achei {obj}"); return; }
            for (var t = go.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
            var b = RoomKit.WorldBounds(go);
            float d = Mathf.Max(0.5f, b.size.magnitude * 1.5f);
            var eye = b.center + (go.transform.parent ? go.transform.parent.TransformDirection(offset.normalized) : offset.normalized) * d;
            using (var s = new RoomKit.Shooter(dir, "ponto", eye, false))
                s.Shot(name, eye, b.center);
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
            Shot(dir, EscritorioBuilder.ScenePath, "EscritorioAzul/Alcapao/Nicho/Pickup_Chave2", "chave_escritorio", new Vector3(0.3f, 0.9f, -1f), "Nicho");
            ShotNamed(dir, SaguaoBuilder.ScenePath, "Pickup_Chave4", "chave_pedestal", new Vector3(0.3f, 0.8f, -1f), "Tampa");
            ShotNamed(dir, SaguaoBuilder.ScenePath, "ChaveInserida_1", "chave_fechadura", new Vector3(-0.6f, 0.2f, -1f));
            ShotNamed(dir, ColinasBuilder.ScenePath, "ChaveColeira", "chave_coleira", new Vector3(0.3f, 0.3f, 1f));
        }
    }
}
