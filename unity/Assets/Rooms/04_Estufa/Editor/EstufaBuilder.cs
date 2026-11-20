using System.Collections.Generic;
using System.IO;
using Somnium.Ambiente;
using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using Somnium.Rooms;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Gera a Estufa (estética do pátio-estufa de Resident Evil): treliça escura no teto com luar, paredes úmidas
    /// com hera, canteiros, banco, fonte central com a planta carnívora (Plant42 do grupo) enrolada na taça onde
    /// estão a Chave 3 e a Metade B do Medalhão. Bolsas da planta pulsam com brilho esverdeado (bioluminescência discreta).
    /// Puzzle: Cartucho Vazio (bancada) → Herbicida (galão do Escritório) → tanque do painel de irrigação →
    /// sprinklers soltam névoa → a planta murcha → taça liberada.
    /// Luz assada (baked). Gerar de novo sobrescreve a cena.
    /// </summary>
    public static class EstufaBuilder
    {
        public const string Folder = "Assets/Rooms/04_Estufa";
        public const string ScenePath = Folder + "/estufa.unity";
        const string GenFolder = Folder + "/Gerado";
        const string ModelFolder = Folder + "/Models";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";

        public static readonly Vector3 Origin = new Vector3(5000f, 0f, 0f);

        const float HalfW = 5f, HalfD = 6f, H = 4.5f, T = 0.25f;
        const float DoorHalfW = 0.6f, DoorH = 2.3f;
        static readonly Vector3 FountainPos = new Vector3(0f, 0f, 1.2f);
        const float BasinHalf = 1.5f, BasinH = 0.5f, BowlY = 1.35f;
        static readonly Vector3 PanelPos = new Vector3(HalfW, 0f, -1.4f);
        static readonly Vector3 BenchPos = new Vector3(-HalfW + 0.45f, 0f, -2.4f);
        static readonly Color Lamp = new Color(1f, 0.8f, 0.55f);

        static RoomKit kit;
        static Transform root;
        static int groundLayer;
        static System.Random rnd;
        static float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);

        // ------------------------------------------------------------------ menus

        [MenuItem("Somnium/Salas/Abrir Estufa")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorUtility.DisplayDialog("Estufa", "A cena ainda não existe. Gerar agora?", "Gerar", "Cancelar")) return;
                Build();
            }
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [MenuItem("Somnium/Salas/Gerar blockout da Estufa (sobrescreve)")]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Estufa", "A cena já existe. Gerar de novo APAGA as edições feitas à mão nela. Continuar?", "Gerar de novo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Open();
        }

        [MenuItem("Somnium/Salas/Renderizar prévias da Estufa")]
        public static void RenderPreviewsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtility.RevealInFinder(RenderPreviews());
        }

        /// <summary>Batch: regera o Saguão (porta ligada, sem a mesa de demonstração) e a Estufa, com as prévias.</summary>
        public static void BuildWithSaguaoAndPreview()
        {
            SaguaoBuilder.Build();
            Build();
            SaguaoBuilder.RenderPreviews();
            RenderPreviews();
        }

        /// <summary>Batch: gera só a Estufa e renderiza as prévias.</summary>
        public static void BuildAndPreview()
        {
            Build();
            RenderPreviews();
        }

        // ------------------------------------------------------------------ geração

        public static void Build()
        {
            kit = new RoomKit(Folder + "/Materials");
            RoomKit.EnsureFolder(GenFolder);
            groundLayer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));
            rnd = new System.Random(19);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.11f, 0.13f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.04f;
            RenderSettings.fogColor = new Color(0.06f, 0.08f, 0.07f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            root = new GameObject("Estufa").transform;
            root.position = Origin;
            root.gameObject.AddComponent<RoomSettings>().roomName = "Estufa";

            BuildShell();
            BuildLattice();
            BuildIvy();
            BuildPlanters();
            BuildBenchAndProps();
            var plant = BuildFountainAndPlant();
            BuildIrrigation(plant);
            BuildLights();

            var spawn = RoomKit.Child(root, "PlayerSpawn", new Vector3(0f, 0f, -HalfD + 1.2f));
            spawn.gameObject.AddComponent<PlayerSpawnPoint>().id = "inicio";

            EditorSceneManager.SaveScene(scene, ScenePath);
            RoomKit.AddToBuildSettings(ScenePath);

            var probeArea = new Bounds(new Vector3(0f, H / 2f, 0f), new Vector3(HalfW * 2f - 1f, H - 0.6f, HalfD * 2f - 1f));
            RoomKit.BakeLighting(root, ScenePath, probeArea, 2f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Estufa salva em {ScenePath}.");
        }

        // ------------------------------------------------------------------ casca: piso de pedra, paredes úmidas, portas

        static void BuildShell()
        {
            var stucco = kit.Mat("Estufa_Reboco", new Color(0.33f, 0.34f, 0.27f), 0.15f);
            var moss = kit.Mat("Estufa_Musgo", new Color(0.17f, 0.21f, 0.13f), 0.1f);
            var floor = kit.Checker("Estufa_Piso", new Color(0.31f, 0.32f, 0.28f), new Color(0.25f, 0.26f, 0.23f), 0.3f);
            var g = RoomKit.Child(root, "Casca", Vector3.zero);

            var fl = RoomKit.Child(g, "Piso", Vector3.zero);
            float[] xs = { -HalfW, -1.7f, 1.7f, HalfW };
            float[] zs = { -HalfD, -2f, 2f, HalfD };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    RoomKit.FloorQuad(fl, $"Piso_{i}{j}", xs[i], xs[i + 1], zs[j], zs[j + 1], 0f, 1.2f, floor, groundLayer);

            void WallX(string n, float z, float x0, float x1, float y0, float y1) =>
                RoomKit.Box(g, n, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), stucco);
            float zs0 = -HalfD - T / 2f;
            WallX("Parede_Norte", HalfD + T / 2f, -HalfW - T, HalfW + T, 0f, H);
            WallX("Parede_Sul_Esq", zs0, -HalfW - T, -DoorHalfW, 0f, H);
            WallX("Parede_Sul_Dir", zs0, DoorHalfW, HalfW + T, 0f, H);
            WallX("Parede_Sul_Topo", zs0, -DoorHalfW, DoorHalfW, DoorH, H);
            RoomKit.Box(g, "Parede_Oeste", new Vector3(-HalfW - T / 2f, H / 2f, 0f), new Vector3(T, H, HalfD * 2f), stucco);
            RoomKit.Box(g, "Parede_Leste", new Vector3(HalfW + T / 2f, H / 2f, 0f), new Vector3(T, H, HalfD * 2f), stucco);

            // Faixa de umidade/musgo na base das paredes.
            var band = RoomKit.Child(g, "Umidade", Vector3.zero);
            RoomKit.Box(band, "Norte", new Vector3(0f, 0.45f, HalfD - 0.01f), new Vector3(HalfW * 2f, 0.9f, 0.02f), moss, false);
            RoomKit.Box(band, "Oeste", new Vector3(-HalfW + 0.01f, 0.45f, 0f), new Vector3(0.02f, 0.9f, HalfD * 2f), moss, false);
            RoomKit.Box(band, "Leste", new Vector3(HalfW - 0.01f, 0.45f, 0f), new Vector3(0.02f, 0.9f, HalfD * 2f), moss, false);
            foreach (float s in new[] { -1f, 1f })
                RoomKit.Box(band, "Sul", new Vector3(s * (HalfW + DoorHalfW) / 2f, 0.45f, -HalfD + 0.01f), new Vector3(HalfW - DoorHalfW, 0.9f, 0.02f), moss, false);
            // Manchas escorridas (água da treliça).
            var stain = kit.Mat("Estufa_Mancha", new Color(0.2f, 0.22f, 0.17f), 0.1f);
            for (int i = 0; i < 10; i++)
            {
                float z = R(-HalfD + 0.6f, HalfD - 0.6f), len = R(1f, 2.6f);
                float x = i % 2 == 0 ? -HalfW + 0.015f : HalfW - 0.015f;
                RoomKit.Box(band, "Escorrido", new Vector3(x, H - len / 2f - 0.1f, z), new Vector3(0.02f, len, R(0.15f, 0.45f)), stain, false);
            }

            // Porta de entrada (volta ao Saguão): madeira gasta de duas folhas.
            var wood = kit.Mat("Estufa_PortaMadeira", new Color(0.36f, 0.33f, 0.28f), 0.2f);
            var d = RoomKit.Child(g, "Porta_Saguao", new Vector3(0f, 0f, -HalfD - 0.05f));
            var leaf = RoomKit.Box(d, "Folhas", new Vector3(0f, DoorH / 2f, 0f), new Vector3(DoorHalfW * 2f, DoorH, 0.06f), wood);
            foreach (float s in new[] { -0.3f, 0.3f })
                foreach (float y in new[] { 0.7f, 1.65f })
                    RoomKit.Box(d, "Almofada", new Vector3(s, y, 0.04f), new Vector3(0.44f, 0.7f, 0.02f), wood, false);
            var back = leaf.AddComponent<RoomDoor>();
            back.targetScenePath = SaguaoBuilder.ScenePath;
            back.spawnId = SaguaoBuilder.SpawnFromEstufa;
            RoomKit.SetPrompt(back, "Sair (voltar ao Saguão)");

            // Porta do fundo (como na referência): trancada, travada por raízes.
            var n = RoomKit.Child(g, "Porta_Fundo", new Vector3(0f, 0f, HalfD - 0.05f));
            var nl = RoomKit.Box(n, "Folhas", new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 2.5f, 0.08f), wood);
            foreach (float s in new[] { -0.37f, 0.37f })
                foreach (float y in new[] { 0.75f, 1.8f })
                    RoomKit.Box(n, "Almofada", new Vector3(s, y, -0.05f), new Vector3(0.55f, 0.8f, 0.02f), wood, false);
            var msg = nl.AddComponent<MessageInteractable>();
            msg.message = "Trancada. Raízes grossas cresceram pelas frestas e travaram a porta.";
            RoomKit.SetPrompt(msg, "Abrir a porta");
        }

        // ------------------------------------------------------------------ teto de treliça com o céu noturno acima

        static void BuildLattice()
        {
            var metal = kit.Mat("Estufa_Trelica", new Color(0.09f, 0.09f, 0.08f), 0.3f, 0.2f);
            var g = RoomKit.Child(root, "Trelica", Vector3.zero);
            var parts = new List<Matrix4x4>();
            for (float x = -HalfW; x <= HalfW + 0.01f; x += 0.8f)
                parts.Add(Matrix4x4.TRS(new Vector3(x, H - 0.06f, 0f), Quaternion.identity, new Vector3(0.06f, 0.12f, HalfD * 2f)));
            for (float z = -HalfD; z <= HalfD + 0.01f; z += 0.8f)
                parts.Add(Matrix4x4.TRS(new Vector3(0f, H - 0.06f, z), Quaternion.identity, new Vector3(HalfW * 2f, 0.12f, 0.06f)));
            // Vigas maiores.
            foreach (float z in new[] { -3.2f, 0f, 3.2f })
                parts.Add(Matrix4x4.TRS(new Vector3(0f, H - 0.18f, z), Quaternion.identity, new Vector3(HalfW * 2f, 0.24f, 0.14f)));
            RoomKit.Combined(g, "Grade", PrimitiveType.Cube, parts, metal);

            // Céu noturno nublado acima da treliça (sem sombra: a luz da lua passa).
            var sky = kit.Emissive("Estufa_CeuNoite", new Color(0.16f, 0.2f, 0.26f), 0.9f);
            var q = RoomKit.Prim(PrimitiveType.Quad, g, "Ceu", new Vector3(0f, H + 0.5f, 0f), new Vector3(HalfW * 2f + 1f, HalfD * 2f + 1f, 1f), sky);
            q.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            q.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            // Vidros quebrados/sujos em alguns vãos (cinza-esverdeado), deixando outros abertos.
            var glass = kit.Mat("Estufa_VidroSujo", new Color(0.2f, 0.24f, 0.22f), 0.6f);
            var gp = new List<Matrix4x4>();
            for (int i = 0; i < 26; i++)
            {
                float x = -HalfW + 0.4f + 0.8f * Mathf.Floor(R(0f, 12.4f));
                float z = -HalfD + 0.4f + 0.8f * Mathf.Floor(R(0f, 14.9f));
                gp.Add(Matrix4x4.TRS(new Vector3(x, H + 0.02f, z), Quaternion.identity, new Vector3(0.74f, 0.01f, 0.74f)));
            }
            RoomKit.Combined(g, "Vidros", PrimitiveType.Cube, gp, glass);
        }

        // ------------------------------------------------------------------ hera cobrindo paredes e pendendo da treliça

        static void BuildIvy()
        {
            var ivyA = kit.Mat("Estufa_Hera", new Color(0.14f, 0.24f, 0.11f), 0.2f);
            var g = RoomKit.Child(root, "Hera", Vector3.zero);
            var parts = new List<Matrix4x4>();
            void Leaf(Vector3 p, Quaternion wallRot, float s) =>
                parts.Add(Matrix4x4.TRS(p, wallRot * Quaternion.Euler(R(-25f, 25f), R(-25f, 25f), R(0f, 360f)), new Vector3(s, s * 0.85f, 0.015f)));

            // Manchas de hera subindo das bordas das paredes (densas no alto e nos cantos).
            var walls = new (Vector3 origin, Vector3 along, Quaternion rot, float len)[]
            {
                (new Vector3(-HalfW, 0f, HalfD - 0.04f), Vector3.right, Quaternion.identity, HalfW * 2f),
                (new Vector3(-HalfW + 0.04f, 0f, -HalfD), Vector3.forward, Quaternion.Euler(0f, 90f, 0f), HalfD * 2f),
                (new Vector3(HalfW - 0.04f, 0f, -HalfD), Vector3.forward, Quaternion.Euler(0f, -90f, 0f), HalfD * 2f),
                (new Vector3(-HalfW, 0f, -HalfD + 0.04f), Vector3.right, Quaternion.identity, HalfW * 2f),
            };
            foreach (var (o, along, rot, len) in walls)
            {
                int patches = Mathf.RoundToInt(len / 1.6f);
                for (int p = 0; p < patches; p++)
                {
                    float u0 = R(0.3f, len - 0.3f);
                    bool fromTop = R(0f, 1f) < 0.65f;
                    float h = R(1.2f, 3.2f);
                    int leaves = Mathf.RoundToInt(h * 22f);
                    for (int i = 0; i < leaves; i++)
                    {
                        float t = R(0f, 1f);
                        float y = fromTop ? H - 0.1f - t * h : 0.9f + t * h * 0.6f;
                        float spread = (fromTop ? (1f - t) : t) * 0.9f + 0.2f;
                        float u = Mathf.Clamp(u0 + R(-spread, spread), 0.05f, len - 0.05f);
                        Leaf(o + along * u + new Vector3(0f, y, 0f), rot, R(0.08f, 0.15f));
                    }
                }
            }
            // Fios de hera pendendo da treliça.
            for (int s = 0; s < 14; s++)
            {
                var top = new Vector3(R(-HalfW + 0.4f, HalfW - 0.4f), H - 0.12f, R(-HalfD + 0.4f, HalfD - 0.4f));
                if (Vector2.Distance(new Vector2(top.x, top.z), new Vector2(FountainPos.x, FountainPos.z)) < 1.2f) continue;
                float len = R(0.6f, 1.7f);
                for (float d = 0f; d < len; d += 0.09f)
                    Leaf(top + new Vector3(R(-0.06f, 0.06f), -d, R(-0.06f, 0.06f)), Quaternion.Euler(0f, R(0f, 360f), 0f), R(0.07f, 0.12f));
            }
            RoomKit.Combined(g, "Folhas", PrimitiveType.Cube, parts, ivyA);
        }

        // ------------------------------------------------------------------ canteiros com vegetação alta e seca

        static void BuildPlanters()
        {
            var brick = kit.Mat("Estufa_Tijolo", new Color(0.3f, 0.2f, 0.16f), 0.15f);
            var soil = kit.Mat("Estufa_Terra", new Color(0.12f, 0.09f, 0.07f), 0.1f);
            var grass = kit.Mat("Estufa_MatoSeco", new Color(0.36f, 0.38f, 0.24f), 0.15f);
            var g = RoomKit.Child(root, "Canteiros", Vector3.zero);
            var planters = new[]
            {
                (new Vector3(-3.3f, 0f, -0.5f), new Vector2(1.6f, 4.2f)),
                (new Vector3(3.1f, 0f, 3.6f), new Vector2(2.6f, 1.4f)),
                (new Vector3(-3.0f, 0f, 4.4f), new Vector2(2.8f, 1.2f)),
            };
            var blades = new List<Matrix4x4>();
            foreach (var (pos, size) in planters)
            {
                var p = RoomKit.Child(g, "Canteiro", pos);
                const float h = 0.45f, w = 0.12f;
                RoomKit.Box(p, "Borda_A", new Vector3(0f, h / 2f, size.y / 2f - w / 2f), new Vector3(size.x, h, w), brick);
                RoomKit.Box(p, "Borda_B", new Vector3(0f, h / 2f, -size.y / 2f + w / 2f), new Vector3(size.x, h, w), brick);
                RoomKit.Box(p, "Borda_C", new Vector3(size.x / 2f - w / 2f, h / 2f, 0f), new Vector3(w, h, size.y), brick);
                RoomKit.Box(p, "Borda_D", new Vector3(-size.x / 2f + w / 2f, h / 2f, 0f), new Vector3(w, h, size.y), brick);
                RoomKit.Box(p, "Terra", new Vector3(0f, h - 0.06f, 0f), new Vector3(size.x - 2 * w, 0.04f, size.y - 2 * w), soil, false);
                int n = Mathf.RoundToInt(size.x * size.y * 55f);
                for (int i = 0; i < n; i++)
                {
                    var b = pos + new Vector3(R(-size.x / 2f + w, size.x / 2f - w), h - 0.05f, R(-size.y / 2f + w, size.y / 2f - w));
                    float bh = R(0.35f, 1.0f);
                    var rot = Quaternion.Euler(R(-28f, 28f), R(0f, 360f), R(-28f, 28f));
                    blades.Add(Matrix4x4.TRS(b + rot * new Vector3(0f, bh / 2f, 0f), rot, new Vector3(0.025f, bh, 0.006f)));
                }
            }
            var mato = RoomKit.Combined(g, "Mato", PrimitiveType.Cube, blades, grass);
            mato.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ banco, carretel de mangueira, bancada com o Cartucho

        static void BuildBenchAndProps()
        {
            var wood = kit.Mat("Estufa_MadeiraBanco", new Color(0.36f, 0.27f, 0.18f), 0.25f);
            var iron = kit.Mat("Estufa_Ferro", new Color(0.18f, 0.18f, 0.17f), 0.4f, 0.3f);
            var clay = kit.Mat("Estufa_Barro", new Color(0.5f, 0.27f, 0.17f), 0.2f);
            var tin = kit.Mat("Estufa_Lata", new Color(0.5f, 0.52f, 0.5f), 0.5f, 0.4f);
            var g = RoomKit.Child(root, "Moveis", Vector3.zero);

            // Banco de ripas (canto sudeste, como na referência).
            var bench = RoomKit.Child(g, "Banco", new Vector3(HalfW - 0.5f, 0f, -4.2f));
            bench.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 4; i++)
                RoomKit.Box(bench, "Ripa", new Vector3(0f, 0.45f, -0.18f + i * 0.12f), new Vector3(1.8f, 0.04f, 0.1f), wood);
            foreach (float x in new[] { -0.75f, 0.75f })
                RoomKit.Box(bench, "Pe", new Vector3(x, 0.22f, 0f), new Vector3(0.06f, 0.44f, 0.46f), iron);

            // Bancada de jardinagem na parede oeste, com o Cartucho Vazio.
            var b = RoomKit.Child(g, "Bancada", BenchPos);
            b.localRotation = Quaternion.Euler(0f, 90f, 0f); // frente para +X (para dentro)
            RoomKit.Box(b, "Tampo", new Vector3(0f, 0.86f, 0f), new Vector3(2.0f, 0.06f, 0.65f), wood);
            RoomKit.Box(b, "Prateleira", new Vector3(0f, 0.3f, 0f), new Vector3(1.9f, 0.04f, 0.6f), wood);
            foreach (float x in new[] { -0.92f, 0.92f })
                foreach (float z in new[] { -0.28f, 0.28f })
                    RoomKit.Box(b, "Perna", new Vector3(x, 0.42f, z), new Vector3(0.06f, 0.84f, 0.06f), wood);
            for (int i = 0; i < 5; i++)
                RoomKit.Prim(PrimitiveType.Cylinder, b, "Vaso", new Vector3(-0.85f + i * 0.2f, 0.98f, 0.12f + (i % 2) * 0.12f), new Vector3(0.14f, 0.1f, 0.14f), clay);
            for (int i = 0; i < 4; i++)
                RoomKit.Prim(PrimitiveType.Cylinder, b, "Vaso_Prateleira", new Vector3(-0.7f + i * 0.45f, 0.42f, 0f), new Vector3(0.22f, 0.12f, 0.22f), clay);
            RoomKit.Box(b, "Pa", new Vector3(0.2f, 0.9f, -0.15f), new Vector3(0.28f, 0.015f, 0.07f), iron, false).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            // Pulverizador antigo sem o reservatório (dica de que falta o cartucho).
            var spr = RoomKit.Child(b, "Pulverizador", new Vector3(0.55f, 0.89f, -0.1f));
            RoomKit.Box(spr, "Corpo", new Vector3(0f, 0.12f, 0f), new Vector3(0.1f, 0.24f, 0.1f), tin, false);
            RoomKit.Box(spr, "Bico", new Vector3(0.12f, 0.2f, 0f), new Vector3(0.18f, 0.02f, 0.02f), tin, false);
            RoomKit.Box(spr, "Encaixe_Vazio", new Vector3(-0.08f, 0.08f, 0f), new Vector3(0.06f, 0.1f, 0.06f), iron, false);

            // Cartucho Vazio (item): pequeno cilindro metálico com brilho.
            var cart = RoomKit.Child(b, "Pickup_CartuchoVazio", new Vector3(0.8f, 0.89f, 0.15f));
            RoomKit.Prim(PrimitiveType.Cylinder, cart, "Cartucho", new Vector3(0f, 0.07f, 0f), new Vector3(0.07f, 0.07f, 0.07f), tin);
            RoomKit.Prim(PrimitiveType.Cylinder, cart, "Tampa", new Vector3(0f, 0.145f, 0f), new Vector3(0.05f, 0.012f, 0.05f), iron);
            var cc = cart.gameObject.AddComponent<BoxCollider>(); cc.center = new Vector3(0f, 0.08f, 0f); cc.size = new Vector3(0.3f, 0.3f, 0.3f); cc.isTrigger = true;
            var cp = cart.gameObject.AddComponent<ItemPickup>(); cp.item = RoomKit.Item("CartuchoVazio"); cp.saveId = "estufa/cartucho";
            var glint = RoomKit.Prim(PrimitiveType.Quad, cart, "Brilho", new Vector3(0.03f, 0.18f, 0f), Vector3.one * 0.1f, kit.Emissive("Estufa_Brilho", new Color(1f, 0.97f, 0.85f), 3f));
            glint.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glint.AddComponent<Brilho>().maxScale = 0.1f;

            // Bilhete do jardineiro (pista do herbicida e da irrigação).
            var note = RoomKit.Box(b, "Bilhete", new Vector3(-0.3f, 0.892f, -0.18f), new Vector3(0.21f, 0.004f, 0.28f), kit.Mat("Estufa_Papel", new Color(0.78f, 0.75f, 0.62f), 0.1f));
            note.GetComponent<BoxCollider>().size = new Vector3(1.6f, 40f, 1.4f);
            var nm = note.AddComponent<MessageInteractable>();
            nm.message = "\"Aquela coisa na fonte não para de crescer. Um herbicida forte no tanque da irrigação deve resolver. O ácido do almoxarifado do escritório serve, num cartucho.\"";
            nm.duration = 7f;
            RoomKit.SetPrompt(nm, "Ler bilhete");
        }

        // ------------------------------------------------------------------ fonte com a planta (Chave 3 + Metade B na taça)

        static PlantaMurcha BuildFountainAndPlant()
        {
            var stone = kit.Mat("Estufa_Pedra", new Color(0.36f, 0.36f, 0.33f), 0.25f);
            var darkStone = kit.Mat("Estufa_PedraEscura", new Color(0.16f, 0.16f, 0.15f), 0.35f);
            var water = kit.Mat("Estufa_Agua", new Color(0.06f, 0.13f, 0.11f), 0.92f);
            var plantMat = kit.Mat("Estufa_Planta", new Color(0.34f, 0.17f, 0.13f), 0.35f);
            var vineMat = kit.Mat("Estufa_Tentaculo", new Color(0.27f, 0.13f, 0.1f), 0.4f);
            var pod = kit.Emissive("Estufa_Bolsa", new Color(0.4f, 1f, 0.45f), 1.2f);
            var gold = kit.Mat("Estufa_Dourado", new Color(0.86f, 0.66f, 0.3f), 0.55f, 0.35f);
            var g = RoomKit.Child(root, "Fonte", FountainPos);

            // Tanque quadrado com borda e água escura.
            const float w = 0.18f;
            RoomKit.Box(g, "Borda_N", new Vector3(0f, BasinH / 2f, BasinHalf - w / 2f), new Vector3(BasinHalf * 2f, BasinH, w), stone);
            RoomKit.Box(g, "Borda_S", new Vector3(0f, BasinH / 2f, -BasinHalf + w / 2f), new Vector3(BasinHalf * 2f, BasinH, w), stone);
            RoomKit.Box(g, "Borda_L", new Vector3(BasinHalf - w / 2f, BasinH / 2f, 0f), new Vector3(w, BasinH, BasinHalf * 2f), stone);
            RoomKit.Box(g, "Borda_O", new Vector3(-BasinHalf + w / 2f, BasinH / 2f, 0f), new Vector3(w, BasinH, BasinHalf * 2f), stone);
            RoomKit.Box(g, "Agua", new Vector3(0f, 0.32f, 0f), new Vector3(BasinHalf * 2f - 2 * w, 0.02f, BasinHalf * 2f - 2 * w), water, false);
            // Pedestal e taça (como a "birdbath" da referência).
            RoomKit.Box(g, "Pedestal_Base", new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.3f, 0.5f), darkStone);
            RoomKit.Prim(PrimitiveType.Cylinder, g, "Pedestal", new Vector3(0f, 0.9f, 0f), new Vector3(0.22f, 0.32f, 0.22f), darkStone, true);
            RoomKit.Prim(PrimitiveType.Cylinder, g, "Taca_Fundo", new Vector3(0f, BowlY - 0.05f, 0f), new Vector3(0.75f, 0.06f, 0.75f), darkStone, true);
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI / 8f;
                RoomKit.Box(g, "Taca_Borda", new Vector3(Mathf.Cos(a) * 0.4f, BowlY + 0.05f, Mathf.Sin(a) * 0.4f), new Vector3(0.07f, 0.14f, 0.17f), darkStone, false)
                    .transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            }

            // Itens na taça: Chave 3 e Metade B do Medalhão.
            var key = RoomKit.Child(g, "Pickup_Chave3", new Vector3(-0.12f, BowlY + 0.01f, 0.05f));
            key.localRotation = Quaternion.Euler(0f, 30f, 0f);
            RoomKit.Box(key, "Chave", Vector3.zero, new Vector3(0.18f, 0.03f, 0.06f), gold, false);
            var kc = key.gameObject.AddComponent<BoxCollider>(); kc.size = new Vector3(0.3f, 0.2f, 0.25f); kc.isTrigger = true;
            var kp = key.gameObject.AddComponent<ItemPickup>(); kp.item = RoomKit.Item("Chave3"); kp.saveId = "estufa/chave3";
            var half = RoomKit.Child(g, "Pickup_MetadeB", new Vector3(0.14f, BowlY + 0.01f, -0.06f));
            RoomKit.Prim(PrimitiveType.Cylinder, half, "Metade", Vector3.zero, new Vector3(0.14f, 0.008f, 0.14f), gold);
            var hc = half.gameObject.AddComponent<BoxCollider>(); hc.size = new Vector3(0.24f, 0.2f, 0.24f); hc.isTrigger = true;
            var hp = half.gameObject.AddComponent<ItemPickup>(); hp.item = RoomKit.Item("MedalhaoMetadeB"); hp.saveId = "estufa/metadeB";

            // Bloqueio: os tentáculos cobrem a taça (a mira acerta isto antes dos itens).
            var block = RoomKit.Volume(g, "Bloqueio_Tentaculos", new Vector3(0f, BowlY + 0.25f, 0f), new Vector3(1.3f, 0.9f, 1.3f), true);
            var bm = block.AddComponent<MessageInteractable>();
            bm.message = "Os tentáculos se fecham sobre a taça quando você chega perto. Lá dentro brilha uma chave... Isto precisa morrer primeiro.";
            bm.duration = 5f;
            RoomKit.SetPrompt(bm, "Examinar a planta");

            // A planta: modelo do grupo (Plant42) crescendo no tanque, ao lado do pedestal.
            var plantRoot = RoomKit.Child(g, "Planta", Vector3.zero);
            plantRoot.gameObject.AddComponent<NaoAssar>();
            var model = RoomKit.Model($"{ModelFolder}/plant42.fbx", plantRoot, "Plant42", new Vector3(-0.75f, 0.33f, 0.6f), 35f, 2.3f, true, plantMat);
            if (model == null) model = RoomKit.Prim(PrimitiveType.Capsule, plantRoot, "Plant42", new Vector3(-0.75f, 1.2f, 0.6f), new Vector3(0.7f, 1f, 0.7f), plantMat);

            // Tentáculos procedurais enrolados na taça (esferas ao longo de espirais, afinando na ponta).
            var vineParts = new List<Matrix4x4>();
            var podSpots = new List<Vector3>();
            for (int v = 0; v < 7; v++)
            {
                float a0 = v * Mathf.PI * 2f / 7f + R(-0.2f, 0.2f);
                float turns = R(0.6f, 1.1f), height = R(1.0f, 1.9f);
                int n = 46;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (n - 1f);
                    float a = a0 + t * turns * Mathf.PI * 2f;
                    float rad = Mathf.Lerp(0.95f, 0.45f, Mathf.Sin(t * Mathf.PI * 0.8f)) + 0.08f * Mathf.Sin(t * 9f + v);
                    float y = 0.3f + t * height + 0.25f * Mathf.Sin(t * Mathf.PI);
                    var p = new Vector3(Mathf.Cos(a) * rad, y, Mathf.Sin(a) * rad);
                    float s = Mathf.Lerp(0.17f, 0.04f, t);
                    vineParts.Add(Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * s));
                    if (i % 11 == 6) podSpots.Add(p + new Vector3(0f, s * 0.4f, 0f));
                }
            }
            var vines = RoomKit.Combined(plantRoot, "Tentaculos", PrimitiveType.Sphere, vineParts, vineMat);

            // Bolsas bioluminescentes (pulsam) + luz verde fraca. Somem quando a planta morre.
            var pods = RoomKit.Child(plantRoot, "Bolsas", Vector3.zero);
            var podRenderers = new List<Renderer>();
            foreach (var p in podSpots)
                podRenderers.Add(RoomKit.Prim(PrimitiveType.Sphere, pods, "Bolsa", p, new Vector3(0.09f, 0.12f, 0.09f), pod).GetComponent<Renderer>());
            var glow = RoomKit.PointLight(pods, "Luz_Bioluminescente", new Vector3(0f, 1.4f, 0f), new Color(0.45f, 1f, 0.5f), 1.3f, 4.5f);
            var pulse = pods.gameObject.AddComponent<PulsoBrilho>();
            pulse.renderers = podRenderers.ToArray();
            pulse.glowLight = glow;

            var wither = g.gameObject.AddComponent<PlantaMurcha>();
            wither.parts = new[] { model.transform, vines.transform };
            var tint = new List<Renderer>(model.GetComponentsInChildren<Renderer>()) { vines.GetComponent<Renderer>() };
            wither.tint = tint.ToArray();
            wither.disableOnWither = new[] { pods.gameObject, block };
            wither.sound = RoomKit.Sound3D(g.gameObject, WitherClip(), 0.9f, 3f, 25f);
            return wither;
        }

        // ------------------------------------------------------------------ painel de irrigação + canos + sprinklers

        static void BuildIrrigation(PlantaMurcha plant)
        {
            var metal = kit.Mat("Estufa_MetalVerde", new Color(0.22f, 0.3f, 0.25f), 0.4f, 0.3f);
            var pipeMat = kit.Mat("Estufa_Cano", new Color(0.4f, 0.36f, 0.3f), 0.4f, 0.4f);
            var dark = kit.Mat("Estufa_Ferro", new Color(0.18f, 0.18f, 0.17f), 0.4f, 0.3f);
            var red = kit.Mat("Estufa_Registro", new Color(0.55f, 0.1f, 0.08f), 0.35f);
            var hose = kit.Mat("Estufa_Mangueira", new Color(0.12f, 0.2f, 0.45f), 0.4f);
            var dial = kit.Mat("Estufa_Mostrador", new Color(0.85f, 0.83f, 0.74f), 0.3f);
            var g = RoomKit.Child(root, "Irrigacao", Vector3.zero);

            var panel = RoomKit.Child(g, "Painel", PanelPos);
            panel.localRotation = Quaternion.Euler(0f, -90f, 0f); // +Z local para dentro (-X)
            RoomKit.Box(panel, "Placa", new Vector3(0f, 1.3f, 0.04f), new Vector3(1.1f, 1.4f, 0.06f), metal, false);
            RoomKit.SignText(panel, "IRRIGAÇÃO", new Vector3(0f, 1.9f, 0.08f), new Vector2(900f, 140f), 100, new Color(0.9f, 0.85f, 0.6f), 180f);
            var tank = RoomKit.Prim(PrimitiveType.Cylinder, panel, "Tanque", new Vector3(-0.2f, 1.2f, 0.28f), new Vector3(0.4f, 0.32f, 0.4f), metal, true);
            RoomKit.Prim(PrimitiveType.Cylinder, panel, "Bocal", new Vector3(-0.2f, 1.56f, 0.28f), new Vector3(0.12f, 0.05f, 0.12f), dark);
            RoomKit.SignText(panel, "QUÍMICOS", new Vector3(-0.2f, 1.2f, 0.485f), new Vector2(380f, 90f), 60, new Color(0.9f, 0.85f, 0.6f), 180f);
            // Registro (roda vermelha) e manômetro.
            var wheel = RoomKit.Prim(PrimitiveType.Cylinder, panel, "Registro", new Vector3(0.32f, 1.05f, 0.14f), new Vector3(0.24f, 0.02f, 0.24f), red);
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var gauge = RoomKit.Prim(PrimitiveType.Cylinder, panel, "Manometro", new Vector3(0.32f, 1.5f, 0.09f), new Vector3(0.18f, 0.02f, 0.18f), dial);
            gauge.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var needle = RoomKit.Child(panel, "Ponteiro", new Vector3(0.32f, 1.5f, 0.105f));
            needle.localRotation = Quaternion.Euler(0f, 0f, 60f);
            needle.gameObject.AddComponent<NaoAssar>();
            RoomKit.Box(needle, "Agulha", new Vector3(0f, 0.035f, 0f), new Vector3(0.01f, 0.07f, 0.004f), red, false);

            // Carretel de mangueira azul (como na referência).
            var reel = RoomKit.Child(g, "Carretel", PanelPos + new Vector3(-0.35f, 0f, 1.1f));
            RoomKit.Prim(PrimitiveType.Cylinder, reel, "Mangueira", new Vector3(0f, 0.42f, 0f), new Vector3(0.6f, 0.12f, 0.6f), hose).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            foreach (float s in new[] { -0.15f, 0.15f })
                RoomKit.Box(reel, "Suporte", new Vector3(s, 0.3f, 0f), new Vector3(0.03f, 0.6f, 0.5f), dark, false);

            // Cano: do painel sobe pela parede, cruza a treliça até o meio e corre sobre a fonte com 3 sprinklers.
            float pipeY = H - 0.3f;
            var pipe = RoomKit.Child(g, "Canos", Vector3.zero);
            var px = PanelPos.x - 0.12f;
            RoomKit.Box(pipe, "Subida", new Vector3(px, (1.5f + pipeY) / 2f, PanelPos.z + 0.1f), new Vector3(0.07f, pipeY - 1.5f, 0.07f), pipeMat, false);
            RoomKit.Box(pipe, "Ate_Fonte", new Vector3((px + FountainPos.x) / 2f, pipeY, PanelPos.z + 0.1f), new Vector3(px - FountainPos.x, 0.07f, 0.07f), pipeMat, false);
            RoomKit.Box(pipe, "Sobre_Fonte", new Vector3(FountainPos.x, pipeY, (PanelPos.z + 0.1f + FountainPos.z + 1f) / 2f), new Vector3(0.07f, 0.07f, FountainPos.z + 1f - PanelPos.z - 0.1f), pipeMat, false);

            var mist = kit.ParticleMat("Estufa_Nevoa", new Color(0.82f, 0.92f, 0.7f, 0.45f));
            var sprinklers = new List<ParticleSystem>();
            foreach (float dz in new[] { -0.8f, 0f, 0.8f })
            {
                var head = RoomKit.Child(pipe, "Sprinkler", new Vector3(FountainPos.x, pipeY - 0.08f, FountainPos.z + dz));
                RoomKit.Prim(PrimitiveType.Cylinder, head, "Bico", Vector3.zero, new Vector3(0.09f, 0.05f, 0.09f), dark);
                var em = RoomKit.Child(head, "Nevoa", new Vector3(0f, -0.06f, 0f));
                em.localRotation = Quaternion.Euler(90f, 0f, 0f); // o cone emite em +Z local → para baixo
                var ps = em.gameObject.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = 5f;
                main.loop = true;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                main.startColor = new Color(0.85f, 0.95f, 0.72f, 0.4f);
                main.gravityModifier = 0.25f;
                main.maxParticles = 160;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var emission = ps.emission;
                emission.rateOverTime = 55f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 32f;
                shape.radius = 0.04f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
                col.color = grad;
                em.GetComponent<ParticleSystemRenderer>().sharedMaterial = mist;
                sprinklers.Add(ps);
            }

            var irr = panel.gameObject.AddComponent<Irrigacao>();
            irr.sprinklers = sprinklers.ToArray();
            irr.plant = plant;
            irr.gaugeNeedle = needle;
            irr.hiss = RoomKit.Sound3D(pipe.gameObject, HissClip(), 0.6f, 3f, 20f);
            irr.hiss.loop = true;
            irr.hiss.transform.localPosition = new Vector3(FountainPos.x, pipeY, FountainPos.z);

            var area = RoomKit.Volume(panel, "Trava_Tanque", new Vector3(0f, 1.25f, 0.3f), new Vector3(1.2f, 1.3f, 0.6f), true);
            var lockComp = area.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { RoomKit.Item("Herbicida") };
            lockComp.consumeItems = true;
            lockComp.usablePromptText = "Despejar o herbicida no tanque";
            lockComp.lockedMessage = "Um painel de irrigação. O tanque de químicos está vazio. Os canos levam até os sprinklers sobre a fonte.";
            lockComp.unlockedMessage = "Você despeja o herbicida e abre o registro. Os canos estremecem... os sprinklers chiam sobre a fonte.";
            lockComp.saveId = "estufa/irrigacao";
            RoomKit.SetPrompt(lockComp, "Examinar o painel");
            UnityEventTools.AddPersistentListener(lockComp.onUnlocked, irr.Play);
            UnityEventTools.AddPersistentListener(lockComp.onRestoredUnlocked, irr.Snap);
        }

        // ------------------------------------------------------------------ luz: luar pela treliça + lâmpadas velhas

        static void BuildLights()
        {
            var g = RoomKit.Child(root, "Iluminacao", Vector3.zero);
            var moon = new GameObject("Luar").AddComponent<Light>();
            moon.transform.SetParent(g, false);
            moon.type = LightType.Directional;
            moon.color = new Color(0.62f, 0.72f, 0.86f);
            moon.intensity = 0.75f;
            moon.shadows = LightShadows.Soft;
            moon.transform.localRotation = Quaternion.Euler(62f, -35f, 0f);

            var cage = kit.Mat("Estufa_Ferro", new Color(0.18f, 0.18f, 0.17f), 0.4f, 0.3f);
            var bulb = kit.Emissive("Estufa_Lampada", new Color(1f, 0.8f, 0.55f), 2.2f);
            foreach (var (name, pos, intensity) in new[]
            {
                ("Lampada_Porta", new Vector3(0f, 2.75f, -HalfD + 0.18f), 1.5f),
                ("Lampada_Bancada", new Vector3(-HalfW + 0.18f, 2.6f, BenchPos.z), 1.7f),
                ("Lampada_Painel", new Vector3(HalfW - 0.18f, 2.6f, PanelPos.z - 0.8f), 1.9f),
            })
            {
                var l = RoomKit.Child(g, name, pos);
                RoomKit.Prim(PrimitiveType.Sphere, l, "Bulbo", Vector3.zero, Vector3.one * 0.09f, bulb);
                RoomKit.Box(l, "Grade", Vector3.zero, new Vector3(0.14f, 0.16f, 0.14f), cage, false).GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                RoomKit.PointLight(l, "Luz", new Vector3(0f, -0.1f, 0f), Lamp, intensity, 7f);
            }
        }

        // ------------------------------------------------------------------ sons sintetizados

        /// <summary>Chiado dos sprinklers: ruído suave (passa-baixa simples), em loop.</summary>
        static AudioClip HissClip()
        {
            float prev = 0f;
            return RoomKit.SynthClip($"{Folder}/Audio/sprinkler.wav", 2f, (t, r) =>
            {
                float n = (float)r.NextDouble() * 2f - 1f;
                prev = prev * 0.55f + n * 0.45f;
                return prev * (0.85f + 0.15f * Mathf.Sin(t * 13f));
            }, seed: 7);
        }

        /// <summary>Planta morrendo: rangido grave que desce + estalos.</summary>
        static AudioClip WitherClip()
        {
            float phase = 0f, prev = 0f;
            return RoomKit.SynthClip($"{Folder}/Audio/planta_murchando.wav", 2.6f, (t, r) =>
            {
                float f = Mathf.Lerp(140f, 45f, t / 2.6f) * (1f + 0.08f * Mathf.Sin(t * 37f));
                phase += 2f * Mathf.PI * f / 44100f;
                float groan = (Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2.03f)) * Mathf.Exp(-t * 0.6f);
                float n = (float)r.NextDouble() * 2f - 1f;
                prev = prev * 0.8f + n * 0.2f;
                float crack = (r.NextDouble() < 0.0006 ? 1f : 0f) * 3f;
                return groan * 0.7f + prev * 0.35f * Mathf.Exp(-t) + crack;
            }, seed: 8);
        }

        // ------------------------------------------------------------------ prévias

        public static string RenderPreviews()
        {
            string dir = RoomKit.PreviewDir();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Vector3 O(float x, float y, float z) => Origin + new Vector3(x, y, z);
            var F = FountainPos;
            using (var s = new RoomKit.Shooter(dir, "estufa", O(0f, 1.65f, -HalfD + 1.2f), false))
            {
                s.Shot("1_entrada", O(0f, 1.65f, -HalfD + 1.2f), O(F.x, 1.3f, F.z + 1f));
                s.Shot("2_fonte_planta", O(1.6f, 1.65f, F.z - 2.3f), O(F.x, 1.2f, F.z));
                s.Shot("3_painel", O(PanelPos.x - 2.6f, 1.65f, PanelPos.z - 1.2f), O(PanelPos.x, 1.4f, PanelPos.z));
                s.Shot("4_bancada", O(BenchPos.x + 2.2f, 1.65f, BenchPos.z - 1f), O(BenchPos.x, 0.9f, BenchPos.z));
                s.Shot("5_trelica", O(-1.5f, 1.65f, -3.5f), O(0.5f, H, 1f));
                s.Shot("6_fundo", O(2.5f, 1.65f, -4.8f), O(-2f, 1.5f, HalfD));

                var irr = GameObject.Find("Estufa/Irrigacao/Painel").GetComponent<Irrigacao>();
                irr.Snap();
                s.Shot("7_planta_morta", O(1.6f, 1.65f, F.z - 2.3f), O(F.x, 1.2f, F.z));
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[Somnium] Prévias da Estufa salvas em {Path.GetFullPath(dir)}");
            return Path.GetFullPath(dir);
        }
    }
}
