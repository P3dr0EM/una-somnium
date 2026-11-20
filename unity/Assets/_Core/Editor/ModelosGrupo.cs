using Somnium.Items;
using UnityEditor;
using UnityEngine;

namespace Somnium.EditorTools
{
    /// <summary>
    /// Liga os modelos do grupo ao jogo:
    /// - modelo de inspeção 3D (inventário) de cada item que tem modelo;
    /// - configuração de importação das texturas baixadas (tamanho máximo para o celular, normal map).
    /// Os FBX vêm dos .blend em models/ (exportados pelo script do Blender, sem alterar os .blend).
    /// Menu: Somnium → Modelos → Aplicar modelos do grupo. Em batch: ModelosGrupo.ApplyAndBuildAll.
    /// </summary>
    public static class ModelosGrupo
    {
        public const string ItemModels = "Assets/Shared/Models/Itens";
        const string Items = "Assets/Data/Items";

        /// <summary>(item, modelo, tamanho na inspeção em metros)</summary>
        static readonly (string item, string model, float size)[] ItemTable =
        {
            ("Alicate", "alicate.fbx", 0.24f),
            ("ManivelaQuadrada", "manivela.fbx", 0.26f),
            ("CartuchoVazio", "cartucho.fbx", 0.18f),
            ("Herbicida", "herbicida.fbx", 0.22f),
            ("MedalhaoDoSol", "medalhao.fbx", 0.16f),
            ("MedalhaoMetadeA", "medalhao_metade.fbx", 0.15f),
            ("MedalhaoMetadeB", "medalhao_metade.fbx", 0.15f),
        };

        [MenuItem("Somnium/Modelos/Aplicar modelos do grupo")]
        public static void Apply()
        {
            ConfigureTextures();
            ConfigureModelImports();
            int n = 0;
            foreach (var (itemId, model, size) in ItemTable)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>($"{Items}/{itemId}.asset");
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemModels}/{model}");
                if (item == null || go == null)
                {
                    Debug.LogWarning($"[Somnium] Modelo de item não aplicado: {itemId} / {model}");
                    continue;
                }
                item.inspectModel = go;
                item.inspectSize = size;
                EditorUtility.SetDirty(item);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Modelos de inspeção aplicados em {n} itens.");
        }

        /// <summary>Batch: aplica os modelos e regera todas as salas com prévias.</summary>
        public static void ApplyAndBuildAll()
        {
            Apply();
            Rooms.TodasSalas.BuildAllAndPreview();
        }

        /// <summary>
        /// Todos os modelos (FBX/OBJ) das salas e itens: gerar UV de lightmap na importação.
        /// Sem isso, modelos estáticos em salas com luz assada ficam pretos (o bake não tem onde gravar a luz deles).
        /// </summary>
        static void ConfigureModelImports()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Rooms", "Assets/Shared" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter imp) || imp.generateSecondaryUV) continue;
                imp.generateSecondaryUV = true;
                imp.SaveAndReimport();
                n++;
            }
            if (n > 0) Debug.Log($"[Somnium] UV de lightmap ativado em {n} modelo(s).");
        }

        static void ConfigureTextures()
        {
            foreach (var path in new[] { "Assets/Rooms/03_SalaoInvertido/Models/Lustre/lustreColor.png", "Assets/Rooms/03_SalaoInvertido/Models/Lustre/lustreNormal.png" })
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool normal = path.Contains("Normal");
                bool changed = imp.maxTextureSize != 1024 || (normal && imp.textureType != TextureImporterType.NormalMap);
                imp.maxTextureSize = 1024;
                if (normal) imp.textureType = TextureImporterType.NormalMap;
                if (changed) imp.SaveAndReimport();
            }
        }
    }
}
