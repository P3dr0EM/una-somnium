using UnityEditor;
using UnityEditor.SceneManagement;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Regera todas as salas (e assa a luz das internas) e renderiza as prévias.
    /// Use depois de mudanças globais (espaço de cor, render pipeline, materiais compartilhados).
    /// Batch: -executeMethod Somnium.EditorTools.Rooms.TodasSalas.BuildAllAndPreview -previewOut "pasta"
    /// </summary>
    public static class TodasSalas
    {
        [MenuItem("Somnium/Salas/Gerar TODAS as salas (sobrescreve)")]
        public static void BuildAllFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Somnium", "Gerar de novo TODAS as salas apaga as edições feitas à mão nelas. Continuar?", "Gerar todas", "Cancelar")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
        }

        public static void BuildAll()
        {
            SaguaoBuilder.Build();
            ColinasBuilder.Build();
            EscritorioBuilder.Build();
            SalaoBuilder.Build();
        }

        public static void BuildAllAndPreview()
        {
            BuildAll();
            SaguaoBuilder.RenderPreviews();
            ColinasBuilder.RenderPreviews();
            EscritorioBuilder.RenderPreviews();
            SalaoBuilder.RenderPreviews();
        }
    }
}
