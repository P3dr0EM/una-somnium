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
            ("LataDeRacao", "lata_racao.fbx", 0.16f),
            ("Chave1", "chave.fbx", 0.2f),
            ("Chave2", "chave.fbx", 0.2f),
            ("Chave3", "chave.fbx", 0.2f),
            ("Chave4", "chave.fbx", 0.2f),
        };

        /// <summary>Modelos que vieram sem material do Blender (ficam brancos): cor (r, g, b, metálico).</summary>
        static readonly System.Collections.Generic.Dictionary<string, Vector4> NoMaterial = new System.Collections.Generic.Dictionary<string, Vector4>
        {
            { "manivela.fbx", new Vector4(0.3f, 0.29f, 0.28f, 0.6f) }, // ferro escuro
            { "lata_racao.fbx", new Vector4(1f, 1f, 1f, 0.3f) },        // textura do grupo
            { "chave.fbx", new Vector4(0.83f, 0.64f, 0.25f, 0.4f) },     // latão dourado

        };

        /// <summary>Textura de cada modelo com material próprio (as do FBX não eram encontradas na importação).</summary>
        public static readonly System.Collections.Generic.Dictionary<string, string> Textures = new System.Collections.Generic.Dictionary<string, string>
        {
            { "lata_racao.fbx", ItemModels + "/pote_raçãoGato2.png" },
        };

        /// <summary>Prefab do modelo com um material próprio (para a inspeção no inventário).</summary>
        static GameObject PrefabWithMaterial(GameObject model, string file, Vector4 c)
        {
            string dir = ItemModels + "/Prefabs";
            Rooms.RoomKit.EnsureFolder(dir);
            string name = System.IO.Path.GetFileNameWithoutExtension(file);
            string matPath = $"{dir}/{name}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", new Color(c.x, c.y, c.z));
            if (Textures.TryGetValue(file, out var texPath)) mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetFloat("_Metallic", c.w);
            mat.SetFloat("_Smoothness", 0.5f);
            EditorUtility.SetDirty(mat);

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            foreach (var r in inst.GetComponentsInChildren<Renderer>())
                r.sharedMaterials = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Repeat(mat, r.sharedMaterials.Length));
            var prefab = PrefabUtility.SaveAsPrefabAsset(inst, $"{dir}/{name}.prefab");
            Object.DestroyImmediate(inst);
            return prefab;
        }

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
                if (NoMaterial.TryGetValue(model, out var col)) go = PrefabWithMaterial(go, model, col);
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
        /// Todos os modelos (FBX/OBJ) das salas e itens: gerar UV de lightmap na importação. Sem luzes e câmeras do Blender:
        /// FBX com "Lamp" embutida vira luz pontual com sombra na cena (deixou o Escritório todo azul-escuro no build).
        /// Sem isso, modelos estáticos em salas com luz assada ficam pretos (o bake não tem onde gravar a luz deles).
        /// </summary>
        static void ConfigureModelImports()
        {
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Rooms", "Assets/Shared" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter imp)) continue;
                if (imp.generateSecondaryUV && !imp.importLights && !imp.importCameras) continue;
                imp.generateSecondaryUV = true;
                imp.importLights = false;
                imp.importCameras = false;
                imp.SaveAndReimport();
                n++;
            }
            if (n > 0) Debug.Log($"[Somnium] Importação ajustada (UV de lightmap, sem luzes/câmeras) em {n} modelo(s).");
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
