using System.Collections.Generic;
using System.IO;
using Somnium.Core;
using Somnium.Inspection;
using Somnium.Interaction;
using Somnium.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Somnium.EditorTools
{
    /// <summary>Menu "Somnium" na barra do Unity: cria os itens do roteiro e a cena de teste dos sistemas.</summary>
    public static class SomniumMenu
    {
        const string ItemsFolder = "Assets/Data/Items";
        const string TestFolder = "Assets/Rooms/_Teste";
        const string TestScenePath = TestFolder + "/teste_sistemas.unity";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";
        // Longe da área externa, para as duas cenas não se sobreporem.
        static readonly Vector3 TestAreaOrigin = new Vector3(500f, 0f, 500f);

        // id do asset, nome exibido, descrição
        static readonly string[,] RoteiroItems =
        {
            { "Chave1", "Chave 1", "Estava na coleira do gato." },
            { "Chave2", "Chave 2", "Estava presa no alto, na Sala das Piscinas." },
            { "Chave3", "Chave 3", "Estava dentro da planta carnívora." },
            { "Chave4", "Chave 4", "Revelada pela estátua do saguão." },
            { "Chave5", "Chave 5", "Caiu do cofre do Salão Invertido." },
            { "ManivelaQuadrada", "Manivela Quadrada", "Uma manivela de ponta quadrada. Deve encaixar em algum mecanismo." },
            { "LivroAntigo", "Livro Antigo", "Capa dura, pesado demais para ser só papel." },
            { "Alicate", "Alicate", "Afiado o bastante para cortar uma corrente." },
            { "LataDeRacao", "Lata de Ração", "Cheiro forte. Algum animal adoraria isto." },
            { "MedalhaoMetadeA", "Metade do Medalhão", "Metade de um medalhão com um sol gravado." },
            { "MedalhaoMetadeB", "Outra Metade do Medalhão", "A outra metade do medalhão do sol." },
            { "MedalhaoDoSol", "Medalhão do Sol", "As duas metades unidas. Parece encaixar em uma estátua." },
            { "CartuchoVazio", "Cartucho Químico Vazio", "Um cartucho de sistema de irrigação. Está vazio." },
            { "Herbicida", "Cartucho de Herbicida", "Cartucho cheio de ácido. Mortal para plantas." },
        };

        [MenuItem("Somnium/Itens/Criar itens do roteiro")]
        public static void CreateRoteiroItems()
        {
            EnsureFolder(ItemsFolder);
            int created = 0;
            for (int i = 0; i < RoteiroItems.GetLength(0); i++)
            {
                string path = $"{ItemsFolder}/{RoteiroItems[i, 0]}.asset";
                if (AssetDatabase.LoadAssetAtPath<ItemData>(path) != null) continue;
                var item = ScriptableObject.CreateInstance<ItemData>();
                item.EditorSetup(RoteiroItems[i, 1], RoteiroItems[i, 2]);
                AssetDatabase.CreateAsset(item, path);
                created++;
            }

            // Combinação: as duas metades viram o Medalhão do Sol.
            var a = LoadItem("MedalhaoMetadeA");
            var b = LoadItem("MedalhaoMetadeB");
            var sol = LoadItem("MedalhaoDoSol");
            if (a != null && b != null && sol != null && !a.combinations.Exists(c => c.with == b))
            {
                a.combinations.Add(new ItemData.Combination { with = b, result = sol, message = "As metades se encaixam: Medalhão do Sol." });
                EditorUtility.SetDirty(a);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Itens do roteiro: {created} criados em {ItemsFolder} (os que já existiam foram mantidos).");
        }

        // ---------- Debug (só no Play) ----------

        [MenuItem("Somnium/Debug/Receber chaves 1-2-3 e o medalhão (a 4 vem da estátua)")]
        static void GiveKeys() => GiveItems("Chave1", "Chave2", "Chave3", "MedalhaoDoSol");

        [MenuItem("Somnium/Debug/Receber Lata de Ração (testar o gato)")]
        static void GiveFood() => GiveItems("LataDeRacao");

        [MenuItem("Somnium/Debug/Receber todos os itens do roteiro")]
        static void GiveAll()
        {
            var ids = new string[RoteiroItems.GetLength(0)];
            for (int i = 0; i < ids.Length; i++) ids[i] = RoteiroItems[i, 0];
            GiveItems(ids);
        }

        [MenuItem("Somnium/Debug/Limpar inventário e estado do mundo")]
        static void ClearState()
        {
            WorldState.Clear();
            Inventory.Instance.Clear();
            Debug.Log("[Somnium] Inventário e WorldState limpos (objetos já alterados na cena só voltam ao recarregar a sala).");
        }

        [MenuItem("Somnium/Debug/Receber chaves 1-2-3 e o medalhão (a 4 vem da estátua)", true)]
        [MenuItem("Somnium/Debug/Receber Lata de Ração (testar o gato)", true)]
        [MenuItem("Somnium/Debug/Receber todos os itens do roteiro", true)]
        [MenuItem("Somnium/Debug/Limpar inventário e estado do mundo", true)]
        static bool IsPlaying() => EditorApplication.isPlaying;

        static void GiveItems(params string[] ids)
        {
            foreach (var id in ids)
            {
                var item = LoadItem(id);
                if (item != null) Inventory.Instance.Add(item);
            }
            Debug.Log($"[Somnium] Itens adicionados ao inventário: {string.Join(", ", ids)}");
        }

        /// <summary>Abre a persistencia (onde está o Player) + a cena de teste. Cria a cena de teste se ainda não existir.</summary>
        [MenuItem("Somnium/Teste/Abrir cena de teste")]
        public static void OpenTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) == null) CreateTestSceneAsset();
            OpenWithPersistencia();
        }

        [MenuItem("Somnium/Teste/Recriar cena de teste")]
        public static void RecreateTestScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateTestSceneAsset();
            OpenWithPersistencia();
        }

        static void OpenWithPersistencia()
        {
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            Debug.Log("[Somnium] Cena de teste aberta junto com a persistencia. Dê Play: o jogador nasce na frente da bancada.");
        }

        /// <summary>
        /// Gera Assets/Rooms/_Teste/teste_sistemas.unity: chão, luz, ponto de spawn e a bancada.
        /// Público para rodar em batch: -executeMethod Somnium.EditorTools.SomniumMenu.CreateTestSceneAsset
        /// </summary>
        public static void CreateTestSceneAsset()
        {
            CreateRoteiroItems();
            EnsureFolder(TestFolder);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var area = new GameObject("Teste_Area");
            area.transform.position = TestAreaOrigin;

            var chao = Box(area.transform, "Chao", new Vector3(0f, -0.05f, 0f), new Vector3(20f, 0.1f, 20f),
                           Mat("Teste_Chao", new Color(0.3f, 0.32f, 0.3f)));
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) chao.layer = groundLayer;

            var luz = new GameObject("Luz");
            luz.transform.SetParent(area.transform, false);
            luz.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);
            var light = luz.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;

            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(area.transform, false);
            spawn.transform.localPosition = new Vector3(0f, 0f, -2.5f);
            spawn.AddComponent<PlayerSpawnPoint>();

            BuildBench(area.transform);

            EditorSceneManager.SaveScene(scene, TestScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Cena de teste salva em {TestScenePath}.");
        }

        static void BuildBench(Transform parent)
        {
            var livro = LoadItem("LivroAntigo");
            var alicate = LoadItem("Alicate");
            livro.inspectModel = CreateBookInspectPrefab(alicate);
            EditorUtility.SetDirty(livro);
            AssetDatabase.SaveAssets();

            var root = new GameObject("Bancada_Teste");
            root.transform.SetParent(parent, false);

            var wood = Mat("Teste_Madeira", new Color(0.4f, 0.25f, 0.12f));
            var metal = Mat("Teste_Metal", new Color(0.45f, 0.47f, 0.5f));
            var gold = Mat("Teste_Dourado", new Color(0.9f, 0.7f, 0.2f));
            var stone = Mat("Teste_Pedra", new Color(0.6f, 0.6f, 0.58f));

            Box(root.transform, "Mesa", new Vector3(0f, 0.4f, 0f), new Vector3(2.4f, 0.8f, 0.8f), wood);

            // 1) Livro: pegar, inspecionar no inventário, achar a trava na lombada e ganhar o Alicate.
            var livroGo = Box(root.transform, "Pickup_LivroAntigo", new Vector3(-0.8f, 0.83f, 0f), new Vector3(0.2f, 0.06f, 0.28f), wood);
            livroGo.AddComponent<ItemPickup>().item = livro;

            // 2) Metades do medalhão: pegar as duas e combinar no inventário.
            var mA = Cylinder(root.transform, "Pickup_MedalhaoMetadeA", new Vector3(-0.2f, 0.82f, 0f), 0.12f, gold);
            mA.AddComponent<ItemPickup>().item = LoadItem("MedalhaoMetadeA");
            var mB = Cylinder(root.transform, "Pickup_MedalhaoMetadeB", new Vector3(0.1f, 0.82f, 0f), 0.12f, gold);
            mB.AddComponent<ItemPickup>().item = LoadItem("MedalhaoMetadeB");

            // 3) Corrente: precisa do Alicate (uso automático) e some ao cortar.
            var corrente = Box(root.transform, "Trava_Corrente", new Vector3(1.6f, 0.6f, 0f), new Vector3(0.1f, 1.2f, 0.6f), metal);
            var lockCorrente = corrente.AddComponent<ItemLock>();
            lockCorrente.requiredItems = new List<ItemData> { alicate };
            lockCorrente.lockedMessage = "Uma corrente grossa. Preciso de algo para cortá-la.";
            lockCorrente.unlockedMessage = "Você corta a corrente com o Alicate.";
            lockCorrente.deactivateOnUnlock = new[] { corrente };
            SetPrompt(lockCorrente, "Examinar corrente");

            // 4) Estátua: precisa do Medalhão do Sol e entrega a Chave 4.
            var estatua = Box(root.transform, "Trava_Estatua", new Vector3(0.6f, 1.05f, 0f), new Vector3(0.3f, 0.5f, 0.3f), stone);
            var lockEstatua = estatua.AddComponent<ItemLock>();
            lockEstatua.requiredItems = new List<ItemData> { LoadItem("MedalhaoDoSol") };
            lockEstatua.lockedMessage = "Há um encaixe redondo com um sol desenhado.";
            lockEstatua.unlockedMessage = "O medalhão encaixa. A estátua se abre: Chave 4!";
            lockEstatua.rewardItem = LoadItem("Chave4");
            SetPrompt(lockEstatua, "Examinar estátua");

        }

        // ---------- helpers ----------

        static GameObject CreateBookInspectPrefab(ItemData reward)
        {
            string path = $"{TestFolder}/Livro_Inspecao.prefab";
            var root = new GameObject("Livro_Inspecao");

            var capa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            capa.name = "Capa";
            capa.transform.SetParent(root.transform, false);
            capa.transform.localScale = new Vector3(0.16f, 0.05f, 0.22f);
            capa.GetComponent<Renderer>().sharedMaterial = Mat("Teste_CapaLivro", new Color(0.35f, 0.08f, 0.08f));

            var paginas = GameObject.CreatePrimitive(PrimitiveType.Cube);
            paginas.name = "Paginas";
            paginas.transform.SetParent(root.transform, false);
            paginas.transform.localPosition = new Vector3(0.005f, 0f, 0f);
            paginas.transform.localScale = new Vector3(0.155f, 0.04f, 0.21f);
            paginas.GetComponent<Renderer>().sharedMaterial = Mat("Teste_Paginas", new Color(0.92f, 0.88f, 0.75f));

            // Trava escondida na lombada (lado -X). Só aparece quando a lombada fica virada para o jogador.
            var trava = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trava.name = "Trava_Lombada";
            trava.transform.SetParent(root.transform, false);
            trava.transform.localPosition = new Vector3(-0.081f, 0f, 0.06f);
            trava.transform.localScale = new Vector3(0.004f, 0.02f, 0.02f);
            trava.GetComponent<Renderer>().sharedMaterial = Mat("Teste_Dourado", new Color(0.9f, 0.7f, 0.2f));
            var hotspot = trava.AddComponent<InspectHotspot>();
            hotspot.id = "trava_lombada";
            hotspot.localFacing = Vector3.left;
            hotspot.prompt = "Pressionar a trava na lombada";
            hotspot.message = "Um fundo falso se abre. Dentro há um Alicate!";
            hotspot.rewardItem = reward;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void SetPrompt(ItemLock target, string text)
        {
            var so = new SerializedObject(target);
            so.FindProperty("promptText").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 localPos, float diameter, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{TestFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static ItemData LoadItem(string id) => AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemsFolder}/{id}.asset");

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
