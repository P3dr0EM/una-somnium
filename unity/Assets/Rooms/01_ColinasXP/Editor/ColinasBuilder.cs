using System.Collections.Generic;
using System.IO;
using System.Linq;
using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using Somnium.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Gera o blockout da Colinas XP: campo verde "infinito" (colinas suaves + neblina + céu com nuvens paradas),
    /// a porta sozinha no meio da grama (volta ao Saguão), o gato que se teletransporta (Chave 1 + Metade do Medalhão
    /// ao dar a Lata de Ração) e a Manivela Quadrada escondida na grama.
    /// Pensado para o celular: terreno ~16 mil triângulos, grama 3D só perto da porta, céu numa textura só.
    /// Depois de gerada, a cena pode ser editada à mão; gerar de novo sobrescreve.
    /// </summary>
    public static class ColinasBuilder
    {
        public const string Folder = "Assets/Rooms/01_ColinasXP";
        public const string ScenePath = Folder + "/colinas_xp.unity";
        const string MatFolder = Folder + "/Materials";
        const string GenFolder = Folder + "/Gerado";
        const string CatModelPath = Folder + "/Models/gato_sentado.obj";
        const string DoorPrefabPath = "Assets/Mechanics/Portal/Prefab/Porta (Portal).prefab";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";
        const string ItemsFolder = "Assets/Data/Items";

        /// <summary>Longe do Saguão (x = 1000) e da área externa.</summary>
        public static readonly Vector3 Origin = new Vector3(2000f, 0f, 0f);

        const float TerrainHalf = 180f;   // terreno de 360 x 360 m
        const int TerrainCells = 90;      // 4 m por célula
        const float FlatRadius = 12f;     // área plana em volta da porta
        const float BoundaryRadius = 46f; // parede invisível
        const float CatRadius = 30f;      // o gato não passa disso

        static readonly Vector3 CatStart = new Vector3(5f, 0f, 15f);
        static readonly Vector3 CrankPos = new Vector3(-9f, 0f, -11f);
        static readonly Color FogColor = new Color(0.74f, 0.85f, 0.97f);

        static int groundLayer;
        static Transform root;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // ------------------------------------------------------------------ menus

        [MenuItem("Somnium/Salas/Abrir Colinas XP")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorUtility.DisplayDialog("Colinas XP", "A cena da Colinas XP ainda não existe. Gerar o blockout agora?", "Gerar", "Cancelar")) return;
                Build();
            }
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Debug.Log("[Somnium] Colinas XP aberta junto com a persistencia. Dê Play: você sai da porta, de frente para o campo.");
        }

        [MenuItem("Somnium/Salas/Gerar blockout da Colinas XP (sobrescreve)")]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Colinas XP", "A cena já existe. Gerar de novo APAGA as edições feitas à mão nela. Continuar?", "Gerar de novo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Open();
        }

        [MenuItem("Somnium/Salas/Renderizar prévias da Colinas XP")]
        public static void RenderPreviewsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtility.RevealInFinder(RenderPreviews());
        }

        /// <summary>Batch: gera a cena e renderiza as prévias ("-previewOut pasta").</summary>
        public static void BuildAndPreview()
        {
            Build();
            RenderPreviews();
        }

        /// <summary>Batch: regera o Saguão (porta ligada a esta sala) e a Colinas XP, com as prévias das duas.</summary>
        public static void BuildBothAndPreview()
        {
            SaguaoBuilder.Build();
            Build();
            SaguaoBuilder.RenderPreviews();
            RenderPreviews();
        }

        // ------------------------------------------------------------------ geração

        public static void Build()
        {
            EnsureFolder(MatFolder);
            EnsureFolder(GenFolder);
            mats.Clear();
            groundLayer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            root = new GameObject("ColinasXP").transform;
            root.position = Origin;
            root.gameObject.AddComponent<RoomSettings>().roomName = "Colinas XP";

            BuildTerrain();
            BuildBoundary();
            var door = BuildDoor();
            BuildGrassTufts();
            BuildCrank();
            BuildCat(door);

            var spawn = Child(root, "PlayerSpawn", new Vector3(0f, 0f, 1.6f));
            var sp = spawn.gameObject.AddComponent<PlayerSpawnPoint>();
            sp.id = "inicio";

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Blockout da Colinas XP salvo em {ScenePath}.");
        }

        static void SetupEnvironment()
        {
            var sky = new Material(Shader.Find("Skybox/Panoramic"));
            sky.SetTexture("_MainTex", SkyTexture());
            sky.SetFloat("_Mapping", 1f);     // latitude-longitude
            sky.SetFloat("_ImageType", 0f);   // 360°
            sky.SetFloat("_Exposure", 1f);
            sky = SaveMaterial(sky, $"{MatFolder}/Colinas_Ceu.mat");
            RenderSettings.skybox = sky;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.74f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.72f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.33f, 0.16f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0085f;
            RenderSettings.fogColor = FogColor;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            var sun = new GameObject("Sol").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(42f, 125f, 0f);
            RenderSettings.sun = sun;
        }

        // ------------------------------------------------------------------ terreno

        /// <summary>Altura do terreno (local): plano perto da porta, colinas suaves depois, bordas subindo para fechar o horizonte.</summary>
        static float Height(float x, float z)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            float big = Mathf.PerlinNoise(x * 0.011f + 37.1f, z * 0.011f + 11.7f) - 0.35f;
            float mid = Mathf.PerlinNoise(x * 0.027f + 5.3f, z * 0.027f + 91.2f) - 0.5f;
            float hills = big * 12f + mid * 3f;
            float w = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(FlatRadius, 48f, r));
            float rim = Mathf.Max(0f, r - 70f) * 0.07f;
            return Mathf.Max(-1f, hills) * w + rim;
        }

        static void BuildTerrain()
        {
            int n = TerrainCells + 1;
            var verts = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            float step = TerrainHalf * 2f / TerrainCells;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -TerrainHalf + i * step, z = -TerrainHalf + j * step;
                    verts[j * n + i] = new Vector3(x, Height(x, z), z);
                    uvs[j * n + i] = new Vector2(x / 4f, z / 4f); // textura repete a cada 4 m
                }
            var tris = new int[TerrainCells * TerrainCells * 6];
            int t = 0;
            for (int j = 0; j < TerrainCells; j++)
                for (int i = 0; i < TerrainCells; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    tris[t++] = a; tris[t++] = c; tris[t++] = b;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            var mesh = new Mesh { name = "Colinas_Terreno", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, $"{GenFolder}/Terreno.asset");

            var go = new GameObject("Terreno");
            go.transform.SetParent(root, false);
            go.layer = groundLayer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = GrassGroundMaterial();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic);
        }

        static void BuildBoundary()
        {
            // Anel de paredes invisíveis: o campo parece infinito, mas o jogador não sai da área da sala.
            var g = Child(root, "Limite", Vector3.zero);
            const int count = 40;
            float seg = 2f * Mathf.PI * BoundaryRadius / count + 0.5f;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * BoundaryRadius;
                p.y = Height(p.x, p.z) + 4f;
                var w = new GameObject($"Parede_{i:00}");
                w.transform.SetParent(g, false);
                w.transform.localPosition = p;
                w.transform.localRotation = Quaternion.LookRotation(-new Vector3(p.x, 0f, p.z));
                w.AddComponent<BoxCollider>().size = new Vector3(seg, 14f, 1f);
            }
        }

        // ------------------------------------------------------------------ porta

        /// <summary>Porta e batente do Caio (do prefab do portal, sem a parte de portal), em pé no meio da grama.</summary>
        static Transform BuildDoor()
        {
            var g = Child(root, "Porta", Vector3.zero);
            Bounds b;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
            if (prefab != null)
            {
                var door = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                PrefabUtility.UnpackPrefabInstance(door, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                door.name = "Modelo";
                door.transform.SetParent(g, false);
                StripPortal(door);
                var white = Mat("Colinas_PortaBranca", new Color(0.86f, 0.85f, 0.8f), 0.25f);
                foreach (var r in door.GetComponentsInChildren<Renderer>(true))
                {
                    r.gameObject.SetActive(true);
                    r.enabled = true; // no prefab do portal a folha fica desligada
                    r.sharedMaterial = white;
                }
                CloseLeaf(door);

                b = WorldBounds(door);
                if (b.size.x < b.size.z) // garante a porta no plano XY (frente para +Z)
                {
                    door.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * door.transform.localRotation;
                    b = WorldBounds(door);
                }
                if (b.size.y < 1.8f || b.size.y > 3.2f) // escala para uma porta de verdade (~2,3 m)
                {
                    door.transform.localScale *= 2.3f / Mathf.Max(0.01f, b.size.y);
                    b = WorldBounds(door);
                }
                door.transform.position += new Vector3(Origin.x - b.center.x, Origin.y - b.min.y, Origin.z - b.center.z);
                b = WorldBounds(door);
                Debug.Log($"[Somnium] Porta da Colinas: {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} m");
            }
            else
            {
                Debug.LogWarning($"[Somnium] Prefab da porta não encontrado em {DoorPrefabPath}; usando blocos.");
                var white = Mat("Colinas_PortaBranca", new Color(0.88f, 0.87f, 0.82f), 0.25f);
                Box(g, "Batente_A", new Vector3(-0.55f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.18f), white);
                Box(g, "Batente_B", new Vector3(0.55f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.18f), white);
                Box(g, "Batente_Topo", new Vector3(0f, 2.25f, 0f), new Vector3(1.2f, 0.1f, 0.18f), white);
                Box(g, "Folha", new Vector3(0f, 1.1f, 0f), new Vector3(1f, 2.2f, 0.05f), white);
                foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                b = WorldBounds(g.gameObject);
            }

            // Uma caixa de colisão (dá a volta em 360°) que também é a interação: atravessar = voltar ao Saguão.
            var area = new GameObject("Passagem_Saguao");
            area.transform.SetParent(g, false);
            area.transform.position = b.center;
            area.AddComponent<BoxCollider>().size = b.size + new Vector3(0f, 0f, 0.1f);
            var roomDoor = area.AddComponent<RoomDoor>();
            roomDoor.targetScenePath = SaguaoBuilder.ScenePath;
            roomDoor.spawnId = SaguaoBuilder.SpawnFromColinas;
            SetPrompt(roomDoor, "Abrir a porta (voltar ao Saguão)");

            BuildDoorDressing(g, b);
            return g;
        }

        static void StripPortal(GameObject door)
        {
            foreach (var t in door.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if (t == null) continue;
                if (t.name == "Tela" || t.GetComponent<Camera>() != null) Object.DestroyImmediate(t.gameObject);
            }
            foreach (var mb in door.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in door.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        /// <summary>No prefab do portal a folha ("porta") está aberta a 90°. Fecha: escolhe o giro que a centraliza no batente ("frame").</summary>
        static void CloseLeaf(GameObject door)
        {
            var all = door.GetComponentsInChildren<Transform>(true);
            var leaf = all.FirstOrDefault(t => t.name == "porta");
            var frame = all.FirstOrDefault(t => t.name == "frame");
            if (leaf == null || frame == null) return;
            var fb = WorldBounds(frame.gameObject);
            var original = leaf.localRotation;
            // 1) Gira até a folha ficar paralela ao batente (o eixo fino dos dois coincide).
            bool frameThinZ = fb.size.z < fb.size.x;
            float best = float.MaxValue, bestYaw = 0f;
            foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
            {
                leaf.localRotation = Quaternion.Euler(0f, yaw, 0f) * original;
                var lb = WorldBounds(leaf.gameObject);
                float thin = frameThinZ ? lb.size.z : lb.size.x;
                if (thin < best - 0.01f) { best = thin; bestYaw = yaw; }
            }
            leaf.localRotation = Quaternion.Euler(0f, bestYaw, 0f) * original;

            // 2) Centraliza a folha no vão do batente (o pivô do modelo não está na dobradiça).
            var leafB = WorldBounds(leaf.gameObject);
            leaf.position += new Vector3(fb.center.x - leafB.center.x, 0f, fb.center.z - leafB.center.z);
        }

        /// <summary>Tijolos soltos na base e um pouco de hera no batente (como na referência).</summary>
        static void BuildDoorDressing(Transform door, Bounds b)
        {
            var brick = Mat("Colinas_Tijolo", new Color(0.62f, 0.3f, 0.2f), 0.1f);
            var ivy = Mat("Colinas_Hera", new Color(0.18f, 0.45f, 0.12f), 0.2f);
            var rnd = new System.Random(7);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);

            var g = Child(door, "Detalhes", Vector3.zero);
            for (int i = 0; i < 9; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                var p = new Vector3(side * R(0.5f, 1.1f), 0.04f + (i > 6 ? 0.08f : 0f), R(-0.5f, 0.6f));
                var br = Box(g, $"Tijolo_{i}", p, new Vector3(0.22f, 0.07f, 0.11f), brick);
                br.transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), R(-8f, 8f));
                Object.DestroyImmediate(br.GetComponent<Collider>());
            }
            float top = b.max.y - Origin.y, right = b.max.x - Origin.x;
            for (int i = 0; i < 7; i++)
            {
                var p = new Vector3(right - R(0f, 0.35f), top - R(0f, 0.9f) * (i % 3 == 0 ? 1.4f : 0.6f), b.max.z - Origin.z + 0.01f);
                var leaf = Box(g, $"Hera_{i}", p, new Vector3(R(0.1f, 0.22f), R(0.1f, 0.22f), 0.03f), ivy);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, R(0f, 90f));
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
            }
        }

        // ------------------------------------------------------------------ grama 3D (só perto da porta e da manivela)

        static void BuildGrassTufts()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var rnd = new System.Random(42);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);

            void Tuft(float x, float z, float scale)
            {
                float y = Height(x, z);
                int blades = 4;
                for (int k = 0; k < blades; k++)
                {
                    float ang = R(0f, Mathf.PI * 2f);
                    float h = R(0.22f, 0.5f) * scale, w = R(0.035f, 0.06f);
                    var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                    var side = Vector3.Cross(Vector3.up, dir) * w;
                    var bse = new Vector3(x + R(-0.08f, 0.08f), y - 0.02f, z + R(-0.08f, 0.08f));
                    var tip = bse + Vector3.up * h + dir * R(0.03f, 0.15f);
                    int i0 = verts.Count;
                    verts.Add(bse - side); verts.Add(bse + side); verts.Add(tip);
                    uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0.5f, 1f));
                    normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);
                    // duas faces (frente e verso)
                    tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 1);
                    tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
                }
            }

            for (int i = 0; i < 420; i++)
            {
                float a = R(0f, Mathf.PI * 2f), r = Mathf.Lerp(1.2f, 16f, Mathf.Sqrt(R(0f, 1f)));
                Tuft(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Lerp(1.2f, 0.8f, r / 16f));
            }
            for (int i = 0; i < 45; i++) // moita onde está a manivela (esconde, mas não demais)
            {
                float a = R(0f, Mathf.PI * 2f), r = R(0.15f, 1.4f);
                Tuft(CrankPos.x + Mathf.Cos(a) * r, CrankPos.z + Mathf.Sin(a) * r, 1.15f);
            }

            var mesh = new Mesh { name = "Colinas_Tufos", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, $"{GenFolder}/Tufos.asset");

            var go = new GameObject("Grama_Tufos");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = BladeMaterial();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ manivela

        static void BuildCrank()
        {
            var iron = Mat("Colinas_Ferro", new Color(0.32f, 0.3f, 0.28f), 0.55f, 0.85f);
            var grip = Mat("Colinas_Cabo", new Color(0.25f, 0.13f, 0.06f), 0.3f);
            var p = CrankPos;
            p.y = Height(p.x, p.z) + 0.03f;
            var g = Child(root, "Pickup_Manivela", p);
            g.localRotation = Quaternion.Euler(0f, 35f, 0f);

            var shaft = Box(g, "Eixo", new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.035f, 0.32f), iron);
            var arm = Box(g, "Braco", new Vector3(0.11f, 0f, 0.16f), new Vector3(0.22f, 0.03f, 0.035f), iron);
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(handle, g, "Cabo", new Vector3(0.22f, 0.05f, 0.16f), new Vector3(0.04f, 0.05f, 0.04f), grip);
            var socket = Box(g, "Encaixe", new Vector3(0f, 0f, -0.17f), new Vector3(0.055f, 0.055f, 0.05f), iron);
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            var col = g.gameObject.AddComponent<BoxCollider>(); // alvo maior para a mira
            col.center = new Vector3(0.08f, 0.1f, 0f);
            col.size = new Vector3(0.6f, 0.35f, 0.6f);
            col.isTrigger = true;
            var pickup = g.gameObject.AddComponent<ItemPickup>();
            pickup.item = Item("ManivelaQuadrada");
            pickup.saveId = "colinas/manivela";

            // Brilho metálico de vez em quando, para dar para achar sem ser óbvio.
            var glint = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Place(glint, g, "Brilho", new Vector3(0.05f, 0.12f, 0.05f), Vector3.one * 0.12f, Unlit("Colinas_Brilho", new Color(1f, 0.98f, 0.85f)));
            Object.DestroyImmediate(glint.GetComponent<Collider>());
            glint.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glint.AddComponent<Brilho>().maxScale = 0.12f;
        }

        // ------------------------------------------------------------------ gato

        static void BuildCat(Transform door)
        {
            var fur = Mat("Colinas_Gato", new Color(0.05f, 0.05f, 0.06f), 0.35f); // preto: contraste forte com o verde (também para daltonismo)
            var collarMat = Mat("Colinas_Coleira", new Color(0.12f, 0.42f, 1f), 0.3f); // azul: legível sobre o preto para os tipos comuns de daltonismo
            var gold = Mat("Colinas_Dourado", new Color(0.85f, 0.65f, 0.25f), 0.7f, 1f);

            var p = CatStart;
            p.y = Height(p.x, p.z);
            var cat = Child(root, "Gato", p);

            // Modelo: escala para ~38 cm sentado e gira para o focinho apontar para +Z (direção do "olhar").
            const float catHeight = 0.38f;
            float headY = 0.9f * catHeight, neckY = 0.66f * catHeight;
            Vector3 neckLocal = new Vector3(0f, neckY, 0.06f);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CatModelPath);
            if (model != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(model);
                m.name = "Modelo";
                m.transform.SetParent(cat, false);
                var mf = m.GetComponentInChildren<MeshFilter>();
                foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterial = fur;

                // Analisa os vértices: a cabeça (topo) fica à frente do centro do corpo.
                var mesh = mf.sharedMesh;
                var v = mesh.vertices;
                float minY = v.Min(q => q.y), maxY = v.Max(q => q.y);
                float h = maxY - minY;
                Vector3 all = Vector3.zero, head = Vector3.zero, neck = Vector3.zero;
                int nh = 0, nn = 0;
                foreach (var q in v)
                {
                    all += q;
                    float k = (q.y - minY) / h;
                    if (k > 0.82f) { head += q; nh++; }
                    if (k > 0.6f && k < 0.72f) { neck += q; nn++; }
                }
                all /= v.Length; head /= Mathf.Max(1, nh); neck /= Mathf.Max(1, nn);
                Vector3 lean = head - all; lean.y = 0f;
                float yaw = lean.sqrMagnitude > 1e-6f ? Mathf.Atan2(lean.x, lean.z) * Mathf.Rad2Deg : 0f;

                float s = catHeight / h;
                var mt = mf.transform;
                mt.localScale = Vector3.one * s;
                mt.localRotation = Quaternion.Euler(0f, -yaw, 0f);
                // Base do modelo no chão, centrada no pivô do gato.
                Vector3 baseCenter = new Vector3(all.x, minY, all.z);
                mt.localPosition = -(mt.localRotation * (baseCenter * s));
                neckLocal = mt.localPosition + mt.localRotation * (neck * s);
                headY = (head.y - minY) * s;
                Debug.Log($"[Somnium] Gato: altura original {h:0.00}, escala {s:0.0000}, giro {-yaw:0}°");
            }
            else
            {
                Debug.LogWarning($"[Somnium] Modelo do gato não encontrado em {CatModelPath}; usando blocos.");
                Box(cat, "Corpo", new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.24f, 0.26f), fur);
                Box(cat, "Cabeca", new Vector3(0f, 0.3f, 0.08f), new Vector3(0.14f, 0.12f, 0.12f), fur);
            }
            foreach (var c in cat.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            // Coleira (anel achatado) com a Chave 1 pendurada.
            var collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(collar, cat, "Coleira", neckLocal, new Vector3(0.11f, 0.008f, 0.11f), collarMat);
            Object.DestroyImmediate(collar.GetComponent<Collider>());
            var key = Box(cat, "ChaveColeira", neckLocal + new Vector3(0f, -0.04f, 0.06f), new Vector3(0.025f, 0.06f, 0.008f), gold);
            Object.DestroyImmediate(key.GetComponent<Collider>());

            var col = cat.gameObject.AddComponent<BoxCollider>(); // alvo da mira (um pouco maior que o gato)
            col.center = new Vector3(0f, headY * 0.5f, 0f);
            col.size = new Vector3(0.45f, headY + 0.1f, 0.5f);

            // Pote de ração (blockout; aparece ao alimentar).
            var bowl = Child(root, "PoteRacao", p + new Vector3(0f, 0f, 0.45f));
            var dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(dish, bowl, "Pote", new Vector3(0f, 0.03f, 0f), new Vector3(0.2f, 0.03f, 0.2f), Mat("Colinas_Pote", new Color(0.2f, 0.35f, 0.75f), 0.5f));
            var food = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(food, bowl, "Racao", new Vector3(0f, 0.05f, 0f), new Vector3(0.16f, 0.012f, 0.16f), Mat("Colinas_Racao", new Color(0.45f, 0.27f, 0.12f), 0.1f));
            foreach (var c in bowl.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            bowl.gameObject.SetActive(false);

            var feed = cat.gameObject.AddComponent<ItemLock>();
            feed.requiredItems = new List<ItemData> { Item("LataDeRacao") };
            feed.consumeItems = true;
            feed.usablePromptText = "Dar ração";
            feed.lockedMessage = "Tem uma chave pendurada na coleira do gato. Ele não deixa ninguém chegar perto.";
            feed.unlockedMessage = "O gato vem comer, tranquilo. Você solta a chave da coleira; tinha mais alguma coisa presa nela.";
            feed.rewardItem = Item("Chave1");
            feed.extraRewardItems = new[] { Item("MedalhaoMetadeA") };
            feed.deactivateOnUnlock = new[] { key };
            feed.activateOnUnlock = new[] { bowl.gameObject };
            feed.saveId = "colinas/gato";
            SetPrompt(feed, "Gato");

            var tele = cat.gameObject.AddComponent<GatoTeleporte>();
            tele.center = door;
            tele.maxDistanceFromCenter = CatRadius;
            tele.groundMask = 1 << groundLayer;
            tele.calmItem = Item("LataDeRacao");
            tele.feedLock = feed;
            tele.bowl = bowl;

            // Miado 3D: vem da direção do gato e cai com a distância (dá para achar o gato pelo som no fone).
            var src = cat.gameObject.AddComponent<AudioSource>();
            src.clip = MeowClip();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 3f;
            src.maxDistance = 60f;
            src.dopplerLevel = 0f;
            src.volume = 0.9f;
            tele.meow = src;
        }

        // ------------------------------------------------------------------ som: miado sintetizado

        /// <summary>
        /// Gera um miado por síntese aditiva ("mi-a-u"): o tom sobe e desce, os harmônicos passam por dois
        /// formantes que vão de "i" para "a" e depois "u", com vibrato leve e um pouco de sopro. Sem licença de terceiros.
        /// Para trocar por um miado gravado, substitua Audio/miado.wav (mesmo nome) ou o clip do AudioSource do gato.
        /// </summary>
        static AudioClip MeowClip()
        {
            string dir = Folder + "/Audio";
            EnsureFolder(dir);
            string path = dir + "/miado.wav";

            const int sr = 44100;
            const float dur = 0.8f;
            int n = (int)(sr * dur);
            var data = new float[n];
            var rnd = new System.Random(11);
            double phase = 0;

            float Lerp3(float p, float a, float b, float c, float pb) =>
                p < pb ? Mathf.Lerp(a, b, Mathf.SmoothStep(0f, 1f, p / pb)) : Mathf.Lerp(b, c, Mathf.SmoothStep(0f, 1f, (p - pb) / (1f - pb)));
            float Formant(float f, float center, float bw) { float x = (f - center) / bw; return Mathf.Exp(-x * x); }

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sr, p = t / dur;
                float f0 = p < 0.2f ? Mathf.Lerp(520f, 820f, Mathf.SmoothStep(0f, 1f, p / 0.2f))
                         : p < 0.55f ? Mathf.Lerp(820f, 760f, (p - 0.2f) / 0.35f)
                         : Mathf.Lerp(760f, 430f, Mathf.SmoothStep(0f, 1f, (p - 0.55f) / 0.45f));
                f0 *= 1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
                phase += 2.0 * Mathf.PI * f0 / sr;

                float f1 = Lerp3(p, 350f, 900f, 450f, 0.35f);
                float f2 = Lerp3(p, 2200f, 1400f, 900f, 0.35f);

                float s = 0f;
                for (int k = 1; k <= 14; k++)
                {
                    float fk = k * f0;
                    if (fk > 8000f) break;
                    float amp = (Formant(fk, f1, 200f) + 0.7f * Formant(fk, f2, 280f) + 0.04f) / Mathf.Pow(k, 0.6f);
                    s += amp * (float)System.Math.Sin(k * phase);
                }

                float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.045f));
                float release = p > 0.68f ? Mathf.Cos((p - 0.68f) / 0.32f * Mathf.PI * 0.5f) : 1f;
                float env = attack * release;
                float breath = ((float)rnd.NextDouble() * 2f - 1f) * 0.03f;
                data[i] = (s + breath) * env;
            }

            float peak = 0.0001f;
            foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            for (int i = 0; i < n; i++) data[i] = data[i] / peak * 0.8f;

            WriteWav(path, data, sr);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void WriteWav(string path, float[] samples, int sampleRate)
        {
            using var fs = new FileStream(path, FileMode.Create);
            using var w = new BinaryWriter(fs);
            int bytes = samples.Length * 2;
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + bytes);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);          // PCM
            w.Write((short)1);          // mono
            w.Write(sampleRate);
            w.Write(sampleRate * 2);    // bytes por segundo
            w.Write((short)2);          // bytes por amostra
            w.Write((short)16);         // bits
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(bytes);
            foreach (var s in samples) w.Write((short)Mathf.Clamp(s * 32767f, -32768f, 32767f));
        }

        // ------------------------------------------------------------------ prévias

        public static string RenderPreviews()
        {
            string dir = "Temp/Previews";
            var args = System.Environment.GetCommandLineArgs();
            int idx = System.Array.IndexOf(args, "-previewOut");
            if (idx >= 0 && idx + 1 < args.Length) dir = args[idx + 1];
            Directory.CreateDirectory(dir);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var cam = new GameObject("PreviewCam").AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.Skybox;
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            float Eye(float x, float z) => Height(x, z) + 1.65f;
            void Shot(string name, Vector3 pos, Vector3 lookAt)
            {
                cam.transform.position = Origin + pos;
                cam.transform.LookAt(Origin + lookAt);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"colinas_{name}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                RenderTexture.active = null;
            }

            cam.transform.position = Origin + new Vector3(0f, 1.65f, 1.6f);
            for (int i = 0; i < 3; i++) cam.Render();

            var cat = CatStart; cat.y = Height(cat.x, cat.z);
            Shot("1_chegada", new Vector3(0f, 1.65f, 1.6f), new Vector3(2f, 1.2f, 30f));
            Shot("2_porta", new Vector3(1.2f, 1.65f, 6.5f), new Vector3(0f, 1.2f, 0f));
            Shot("3_porta_atras", new Vector3(-2.5f, 1.65f, -5f), new Vector3(0f, 1.2f, 0f));
            Shot("4_gato", new Vector3(cat.x - 1.2f, cat.y + 1.1f, cat.z - 2.4f), cat + new Vector3(0f, 0.2f, 0f));
            Shot("5_manivela", new Vector3(CrankPos.x + 1.6f, Eye(CrankPos.x, CrankPos.z), CrankPos.z + 2.2f), new Vector3(CrankPos.x, Height(CrankPos.x, CrankPos.z), CrankPos.z));
            Shot("6_horizonte", new Vector3(0f, 1.65f, 0f), new Vector3(-40f, 4f, -20f));
            Shot("7_alto", new Vector3(-22f, 18f, -26f), new Vector3(0f, 0f, 4f));

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[Somnium] Prévias da Colinas XP salvas em {Path.GetFullPath(dir)}");
            return Path.GetFullPath(dir);
        }

        // ------------------------------------------------------------------ texturas geradas

        static Texture2D SkyTexture()
        {
            string path = $"{GenFolder}/Ceu.png";
            const int w = 2048, h = 1024;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            var horizon = FogColor;
            var zenith = new Color(0.16f, 0.43f, 0.88f);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float elev = (y + 0.5f) / h * Mathf.PI - Mathf.PI / 2f;
                float sinE = Mathf.Sin(elev), cosE = Mathf.Cos(elev);
                Color skyCol = elev <= 0f ? horizon : Color.Lerp(horizon, zenith, Mathf.Pow(sinE, 0.55f));
                for (int x = 0; x < w; x++)
                {
                    Color c = skyCol;
                    if (elev > 0.02f)
                    {
                        float az = (x + 0.5f) / w * Mathf.PI * 2f;
                        // Projeta a direção num "teto" plano de nuvens: nuvens maiores no alto, achatadas no horizonte.
                        float dx = Mathf.Sin(az) * cosE, dz = Mathf.Cos(az) * cosE;
                        float k = 1f / (sinE + 0.12f);
                        float u = dx * k * 2.2f, v = dz * k * 2.2f;
                        float n = Fbm(u + 13.2f, v + 7.7f);
                        float cover = Edge(0.5f, 0.6f, n);
                        cover *= Edge(0.02f, 0.18f, elev); // some perto do horizonte
                        float shade = Mathf.Lerp(0.82f, 1f, Edge(0.56f, 0.85f, Fbm(u * 1.7f + 3.1f, v * 1.7f - 8.4f)));
                        c = Color.Lerp(skyCol, new Color(shade, shade, Mathf.Min(1f, shade + 0.03f)), cover);
                    }
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapModeU = TextureWrapMode.Repeat;
            imp.wrapModeV = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Corte suave estilo shader: 0 abaixo de e0, 1 acima de e1, curva em S no meio.</summary>
        static float Edge(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float x, float y)
        {
            float sum = 0f, amp = 0.5f, f = 1f, norm = 0f;
            for (int o = 0; o < 5; o++)
            {
                sum += Mathf.PerlinNoise(x * f, y * f) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2.03f;
            }
            return sum / norm;
        }

        /// <summary>Grama estilizada, repetível sem emenda (senos com frequências inteiras + riscos).</summary>
        static Material GrassGroundMaterial()
        {
            string path = $"{GenFolder}/Grama.png";
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGB24, true);
            var baseCol = new Color(0.33f, 0.66f, 0.13f);
            var px = new Color[s * s];
            var rnd = new System.Random(3);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = x * 2f * Mathf.PI / s, v = y * 2f * Mathf.PI / s;
                    float n = 0.5f + 0.18f * Mathf.Sin(u * 2 + 1.3f) * Mathf.Cos(v * 3 + 0.4f)
                                   + 0.12f * Mathf.Sin(u * 5 + v * 4 + 2f) + 0.08f * Mathf.Cos(u * 9 - v * 7);
                    px[y * s + x] = baseCol * Mathf.Lerp(0.85f, 1.12f, n);
                }
            for (int i = 0; i < 1400; i++) // riscos curtos (folhas) mais claros e mais escuros
            {
                int x0 = rnd.Next(s), y0 = rnd.Next(s), len = 3 + rnd.Next(6);
                float tone = rnd.NextDouble() < 0.5 ? 0.78f : 1.22f;
                int slant = rnd.Next(3) - 1;
                for (int k = 0; k < len; k++)
                {
                    int xx = ((x0 + (k * slant) / 3) % s + s) % s, yy = (y0 + k) % s;
                    px[yy * s + xx] *= tone;
                }
            }
            for (int i = 0; i < px.Length; i++) px[i].a = 1f;
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            var mat = Mat("Colinas_Grama", Color.white, 0.12f);
            mat.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Folhas dos tufos: degradê de verde escuro (base) a verde-amarelado (ponta).</summary>
        static Material BladeMaterial()
        {
            string path = $"{GenFolder}/Folha.png";
            const int w = 4, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            for (int y = 0; y < h; y++)
            {
                var c = Color.Lerp(new Color(0.16f, 0.38f, 0.07f), new Color(0.55f, 0.8f, 0.2f), y / (h - 1f));
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();

            var mat = Mat("Colinas_Folha", Color.white, 0.1f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ helpers

        static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Place(go, parent, name, localPos, size, mat);
            return go;
        }

        static void Place(GameObject go, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void SetPrompt(Assets.Scripts.Interactable target, string text)
        {
            var so = new SerializedObject(target);
            so.FindProperty("promptText").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static ItemData Item(string id)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemsFolder}/{id}.asset");
            if (item == null) Debug.LogWarning($"[Somnium] Item '{id}' não encontrado. Rode Somnium > Itens > Criar itens do roteiro.");
            return item;
        }

        static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        static Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(LitShader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            mats[name] = mat;
            return mat;
        }

        static Material Unlit(string name, Color color)
        {
            string path = $"{MatFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material SaveMaterial(Material mat, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = mat.shader;
                existing.CopyPropertiesFromMaterial(mat);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
