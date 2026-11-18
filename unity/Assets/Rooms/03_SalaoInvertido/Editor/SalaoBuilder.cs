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
    /// Gera o Salão Invertido: sala de jantar vitoriana de ponta-cabeça (o jogador anda no forro; mesa, cadeiras,
    /// lareira, aparador e relógio ficam "grudados" lá em cima), com lareira de fogo para baixo, velas invertidas,
    /// janelas com o céu embaixo, quadros e relógio invertidos (pêndulo para cima).
    /// Puzzle: Manivela Quadrada no mecanismo da parede → as correntes soltam → o lustre despenca e quebra → Livro Antigo.
    /// Luz assada (baked). Gerar de novo sobrescreve a cena.
    /// </summary>
    public static class SalaoBuilder
    {
        public const string Folder = "Assets/Rooms/03_SalaoInvertido";
        public const string ScenePath = Folder + "/salao_invertido.unity";
        const string GenFolder = Folder + "/Gerado";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";

        public static readonly Vector3 Origin = new Vector3(4000f, 0f, 0f);

        const float HalfW = 5f, HalfD = 7f, H = 5f, T = 0.25f;
        const float DoorHalfW = 0.6f, DoorH = 2.3f;
        static readonly Vector3 LustrePos = new Vector3(0f, 0f, 3.2f);   // xz do lustre
        static readonly Vector3 MechPos = new Vector3(HalfW - 0.12f, 1.3f, 3.2f);
        static readonly Color Warm = new Color(1f, 0.78f, 0.5f);

        static RoomKit kit;
        static Transform root;
        static int groundLayer;

        // ------------------------------------------------------------------ menus

        [MenuItem("Somnium/Salas/Abrir Salão Invertido")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorUtility.DisplayDialog("Salão Invertido", "A cena ainda não existe. Gerar agora?", "Gerar", "Cancelar")) return;
                Build();
            }
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        [MenuItem("Somnium/Salas/Gerar blockout do Salão Invertido (sobrescreve)")]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Salão Invertido", "A cena já existe. Gerar de novo APAGA as edições feitas à mão nela. Continuar?", "Gerar de novo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Open();
        }

        [MenuItem("Somnium/Salas/Renderizar prévias do Salão Invertido")]
        public static void RenderPreviewsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtility.RevealInFinder(RenderPreviews());
        }

        /// <summary>Batch: regera o Saguão (porta ligada, mesa de demo sem o Livro) e o Salão, com as prévias do Salão.</summary>
        public static void BuildWithSaguaoAndPreview()
        {
            SaguaoBuilder.Build();
            Build();
            RenderPreviews();
        }

        // ------------------------------------------------------------------ geração

        public static void Build()
        {
            kit = new RoomKit(Folder + "/Materials");
            RoomKit.EnsureFolder(GenFolder);
            groundLayer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.21f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = new Color(0.08f, 0.07f, 0.06f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            root = new GameObject("SalaoInvertido").transform;
            root.position = Origin;
            root.gameObject.AddComponent<RoomSettings>().roomName = "Salão Invertido";

            BuildShell();
            BuildTableAndChairs();
            BuildFireplace();
            BuildClockAndSideboard();
            BuildPaintings();
            BuildWindows();
            BuildSconces();
            BuildChandelierPuzzle();

            var spawn = RoomKit.Child(root, "PlayerSpawn", new Vector3(0f, 0f, -HalfD + 1.2f));
            spawn.gameObject.AddComponent<PlayerSpawnPoint>().id = "inicio";

            EditorSceneManager.SaveScene(scene, ScenePath);
            RoomKit.AddToBuildSettings(ScenePath);

            var probeArea = new Bounds(new Vector3(0f, 2.5f, 0f), new Vector3(HalfW * 2f - 1f, 4.4f, HalfD * 2f - 1f));
            RoomKit.BakeLighting(root, ScenePath, probeArea, 2.2f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Salão Invertido salvo em {ScenePath}.");
        }

        // ------------------------------------------------------------------ casca: forro (onde se anda), assoalho lá em cima, paredes

        static void BuildShell()
        {
            var plaster = kit.Mat("Salao_Forro", new Color(0.78f, 0.74f, 0.66f), 0.15f);
            var wood = kit.Mat("Salao_Assoalho", new Color(0.3f, 0.17f, 0.08f), 0.35f);
            var wall = kit.Mat("Salao_PapelParede", new Color(0.17f, 0.31f, 0.25f), 0.15f);
            var panel = kit.Mat("Salao_Lambri", new Color(0.24f, 0.13f, 0.06f), 0.35f);
            var rug = kit.Mat("Salao_Tapete", new Color(0.42f, 0.06f, 0.07f), 0.05f);
            var g = RoomKit.Child(root, "Casca", Vector3.zero);

            // Onde o jogador anda: o forro de gesso, em placas (limite de luzes por objeto no celular).
            var floor = RoomKit.Child(g, "Forro_Chao", Vector3.zero);
            float[] xs = { -HalfW, 0f, HalfW };
            float[] zs = { -HalfD, -2.3f, 2.3f, HalfD };
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 3; j++)
                    RoomKit.FloorQuad(floor, $"Placa_{i}{j}", xs[i], xs[i + 1], zs[j], zs[j + 1], 0f, 1f, plaster, groundLayer);
            // Rosácea de gesso no "chão" (no forro real ficaria em volta do lustre).
            RoomKit.Prim(PrimitiveType.Cylinder, g, "Rosacea", new Vector3(0f, 0.01f, -1.5f), new Vector3(1.6f, 0.01f, 1.6f), kit.Mat("Salao_Rosacea", new Color(0.86f, 0.83f, 0.76f), 0.2f));

            // Lá em cima: o assoalho de madeira e o tapete (de ponta-cabeça).
            RoomKit.Box(g, "Assoalho_Teto", new Vector3(0f, H + 0.1f, 0f), new Vector3(HalfW * 2f + 2 * T, 0.2f, HalfD * 2f + 2 * T), wood);
            RoomKit.Box(g, "Tapete_Teto", new Vector3(0f, H - 0.01f, -1.5f), new Vector3(3.6f, 0.02f, 5.2f), rug, false);

            // Paredes: papel de parede verde; o lambri de madeira fica EM CIMA (perto do assoalho invertido);
            // a sanca de gesso fica embaixo, junto do forro onde se anda.
            void WallX(string n, float z, float x0, float x1, float y0, float y1) =>
                RoomKit.Box(g, n, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), wall);
            float zs0 = -HalfD - T / 2f;
            WallX("Parede_Norte", HalfD + T / 2f, -HalfW - T, HalfW + T, 0f, H);
            WallX("Parede_Sul_Esq", zs0, -HalfW - T, -DoorHalfW, 0f, H);
            WallX("Parede_Sul_Dir", zs0, DoorHalfW, HalfW + T, 0f, H);
            WallX("Parede_Sul_Topo", zs0, -DoorHalfW, DoorHalfW, DoorH, H);
            RoomKit.Box(g, "Parede_Oeste", new Vector3(-HalfW - T / 2f, H / 2f, 0f), new Vector3(T, H, HalfD * 2f), wall);
            RoomKit.Box(g, "Parede_Leste", new Vector3(HalfW + T / 2f, H / 2f, 0f), new Vector3(T, H, HalfD * 2f), wall);

            var trim = RoomKit.Child(g, "Lambri_e_Sanca", Vector3.zero);
            var molding = kit.Mat("Salao_Sanca", new Color(0.86f, 0.83f, 0.76f), 0.2f);
            foreach (var (n, pos, size) in new[]
            {
                ("Norte", new Vector3(0f, 0f, HalfD - 0.02f), new Vector3(HalfW * 2f, 0f, 0.04f)),
                ("Oeste", new Vector3(-HalfW + 0.02f, 0f, 0f), new Vector3(0.04f, 0f, HalfD * 2f)),
                ("Leste", new Vector3(HalfW - 0.02f, 0f, 0f), new Vector3(0.04f, 0f, HalfD * 2f)),
            })
            {
                RoomKit.Box(trim, "Lambri_" + n, pos + new Vector3(0f, H - 0.55f, 0f), size + new Vector3(0f, 1.1f, 0f), panel, false);
                RoomKit.Box(trim, "Sanca_" + n, pos + new Vector3(0f, 0.12f, 0f), size + new Vector3(0f, 0.24f, 0.04f), molding, false);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                float cx = side * (HalfW + DoorHalfW) / 2f, w = HalfW - DoorHalfW;
                RoomKit.Box(trim, "Lambri_Sul", new Vector3(cx, H - 0.55f, -HalfD + 0.02f), new Vector3(w, 1.1f, 0.04f), panel, false);
                RoomKit.Box(trim, "Sanca_Sul", new Vector3(cx, 0.12f, -HalfD + 0.02f), new Vector3(w, 0.24f, 0.08f), molding, false);
            }

            // Porta de entrada (volta ao Saguão): funciona normal, mas a maçaneta e a placa estão de ponta-cabeça.
            var doorMat = kit.Mat("Salao_Porta", new Color(0.26f, 0.14f, 0.06f), 0.35f);
            var brass = kit.Mat("Salao_Latao", new Color(0.85f, 0.66f, 0.28f), 0.6f, 0.9f);
            var d = RoomKit.Child(g, "Porta_Saguao", new Vector3(0f, 0f, -HalfD - 0.05f));
            var leaf = RoomKit.Box(d, "Folha", new Vector3(0f, DoorH / 2f, 0f), new Vector3(DoorHalfW * 2f, DoorH, 0.06f), doorMat);
            RoomKit.Prim(PrimitiveType.Sphere, d, "Macaneta", new Vector3(0.42f, DoorH - 0.95f, 0.06f), Vector3.one * 0.07f, brass);
            var sign = RoomKit.SignText(d, "SALÃO", new Vector3(0f, 0.55f, 0.04f), new Vector2(600f, 160f), 110, new Color(0.85f, 0.7f, 0.4f), 180f);
            sign.transform.parent.localRotation = Quaternion.Euler(0f, 180f, 180f); // placa de ponta-cabeça
            var back = leaf.AddComponent<RoomDoor>();
            back.targetScenePath = SaguaoBuilder.ScenePath;
            back.spawnId = SaguaoBuilder.SpawnFromSalao;
            RoomKit.SetPrompt(back, "Sair (voltar ao Saguão)");
        }

        // ------------------------------------------------------------------ mesa de jantar e cadeiras (lá em cima)

        static void BuildTableAndChairs()
        {
            var wood = kit.Mat("Salao_Mogno", new Color(0.32f, 0.12f, 0.05f), 0.45f);
            var cloth = kit.Mat("Salao_Toalha", new Color(0.88f, 0.86f, 0.8f), 0.1f);
            var china = kit.Mat("Salao_Porcelana", new Color(0.92f, 0.92f, 0.95f), 0.6f);
            var brass = kit.Mat("Salao_Latao", new Color(0.85f, 0.66f, 0.28f), 0.6f, 0.9f);
            var wax = kit.Mat("Salao_Cera", new Color(0.93f, 0.9f, 0.8f), 0.2f);
            var flame = kit.Emissive("Salao_Chama", new Color(1f, 0.72f, 0.35f), 3f);
            var g = RoomKit.Child(root, "Mesa_Invertida", new Vector3(0f, 0f, -1.5f));

            float topY = H - 0.76f;
            RoomKit.Box(g, "Tampo", new Vector3(0f, topY, 0f), new Vector3(1.2f, 0.07f, 2.8f), wood, false);
            RoomKit.Box(g, "Toalha", new Vector3(0f, topY - 0.04f, 0f), new Vector3(1.26f, 0.01f, 2.86f), cloth, false);
            foreach (var p in new[] { new Vector3(-0.5f, 0f, -1.3f), new Vector3(0.5f, 0f, -1.3f), new Vector3(-0.5f, 0f, 1.3f), new Vector3(0.5f, 0f, 1.3f) })
                RoomKit.Box(g, "Perna", p + new Vector3(0f, (topY + H) / 2f, 0f), new Vector3(0.08f, H - topY, 0.08f), wood, false);

            // Pratos "pendurados" embaixo da toalha.
            foreach (float z in new[] { -0.9f, 0f, 0.9f })
                foreach (float x in new[] { -0.35f, 0.35f })
                    RoomKit.Prim(PrimitiveType.Cylinder, g, "Prato", new Vector3(x, topY - 0.055f, z), new Vector3(0.26f, 0.008f, 0.26f), china);

            // Candelabro de 3 braços com as velas para baixo (chama embaixo).
            var cand = RoomKit.Child(g, "Candelabro", new Vector3(0f, topY - 0.05f, 0f));
            RoomKit.Prim(PrimitiveType.Cylinder, cand, "Haste", new Vector3(0f, -0.15f, 0f), new Vector3(0.04f, 0.15f, 0.04f), brass);
            RoomKit.Box(cand, "Bracos", new Vector3(0f, -0.3f, 0f), new Vector3(0.5f, 0.03f, 0.03f), brass, false);
            foreach (float x in new[] { -0.24f, 0f, 0.24f })
            {
                RoomKit.Prim(PrimitiveType.Cylinder, cand, "Vela", new Vector3(x, -0.42f, 0f), new Vector3(0.04f, 0.1f, 0.04f), wax);
                RoomKit.Prim(PrimitiveType.Sphere, cand, "Chama", new Vector3(x, -0.55f, 0f), new Vector3(0.035f, 0.07f, 0.035f), flame);
            }
            RoomKit.PointLight(cand, "Luz_Candelabro", new Vector3(0f, -0.65f, 0f), Warm, 2.4f, 6f);

            // Cadeiras: o assento a 45 cm do assoalho (lá em cima) e o encosto apontando para baixo.
            var seats = new List<(Vector3 pos, float yaw)>();
            foreach (float z in new[] { -0.9f, 0f, 0.9f })
            {
                seats.Add((new Vector3(-0.95f, 0f, z), 90f));
                seats.Add((new Vector3(0.95f, 0f, z), -90f));
            }
            seats.Add((new Vector3(0f, 0f, -1.75f), 0f));
            seats.Add((new Vector3(0f, 0f, 1.75f), 180f));
            float seatY = H - 0.46f;
            for (int i = 0; i < seats.Count; i++)
            {
                var c = RoomKit.Child(g, $"Cadeira_{i}", seats[i].pos);
                c.localRotation = Quaternion.Euler(0f, seats[i].yaw, 0f);
                RoomKit.Box(c, "Assento", new Vector3(0f, seatY, 0f), new Vector3(0.45f, 0.05f, 0.45f), wood, false);
                foreach (var p in new[] { new Vector3(-0.19f, 0f, -0.19f), new Vector3(0.19f, 0f, -0.19f), new Vector3(-0.19f, 0f, 0.19f), new Vector3(0.19f, 0f, 0.19f) })
                    RoomKit.Box(c, "Perna", p + new Vector3(0f, (seatY + H) / 2f, 0f), new Vector3(0.04f, H - seatY, 0.04f), wood, false);
                // Encosto do lado de fora da mesa (local -Z), "pendurado" para baixo.
                RoomKit.Box(c, "Encosto", new Vector3(0f, seatY - 0.45f, -0.21f), new Vector3(0.42f, 0.85f, 0.04f), wood, false);
            }
        }

        // ------------------------------------------------------------------ lareira com o fogo para baixo

        static void BuildFireplace()
        {
            var stone = kit.Mat("Salao_Marmore", new Color(0.55f, 0.52f, 0.5f), 0.5f);
            var soot = kit.Mat("Salao_Fuligem", new Color(0.05f, 0.04f, 0.04f), 0.1f);
            var log = kit.Mat("Salao_Lenha", new Color(0.18f, 0.1f, 0.05f), 0.1f);
            var fireMat = kit.Emissive("Salao_Fogo", new Color(1f, 0.45f, 0.12f), 4f);
            var g = RoomKit.Child(root, "Lareira_Invertida", new Vector3(0f, 0f, HalfD));

            // Corpo da lareira de H-1.5 até o assoalho; a "boca" (fornalha) colada no assoalho lá em cima.
            RoomKit.Box(g, "Corpo_Esq", new Vector3(-0.85f, H - 0.75f, -0.25f), new Vector3(0.4f, 1.5f, 0.5f), stone, false);
            RoomKit.Box(g, "Corpo_Dir", new Vector3(0.85f, H - 0.75f, -0.25f), new Vector3(0.4f, 1.5f, 0.5f), stone, false);
            RoomKit.Box(g, "Fornalha", new Vector3(0f, H - 0.55f, -0.12f), new Vector3(1.3f, 1.1f, 0.25f), soot, false);
            RoomKit.Box(g, "Verga", new Vector3(0f, H - 1.25f, -0.25f), new Vector3(2.1f, 0.3f, 0.5f), stone, false);
            // Prateleira (consolo) EMBAIXO da lareira, como se estivesse em cima numa sala normal.
            RoomKit.Box(g, "Consolo", new Vector3(0f, H - 1.45f, -0.32f), new Vector3(2.4f, 0.08f, 0.64f), stone, false);
            RoomKit.Prim(PrimitiveType.Cylinder, g, "Vaso_Consolo", new Vector3(-0.8f, H - 1.66f, -0.35f), new Vector3(0.14f, 0.14f, 0.14f), kit.Mat("Salao_Porcelana", new Color(0.92f, 0.92f, 0.95f), 0.6f));

            // Lenha grudada no "chão" da lareira (lá em cima) e as chamas pendendo para baixo.
            var fire = RoomKit.Child(g, "Fogo", Vector3.zero);
            fire.gameObject.AddComponent<NaoAssar>();
            foreach (float x in new[] { -0.3f, 0.3f })
                RoomKit.Prim(PrimitiveType.Cylinder, fire, "Lenha", new Vector3(x, H - 0.1f, -0.3f), new Vector3(0.12f, 0.3f, 0.12f), log).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            var flames = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                float x = -0.4f + i * 0.2f;
                var f = RoomKit.Prim(PrimitiveType.Sphere, fire, $"Chama_{i}", new Vector3(x, H - 0.45f, -0.3f), new Vector3(0.16f, 0.5f - Mathf.Abs(i - 2) * 0.08f, 0.12f), fireMat);
                f.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                flames.Add(f.transform);
            }
            var light = RoomKit.PointLight(fire, "Luz_Fogo", new Vector3(0f, H - 1f, -0.9f), new Color(1f, 0.55f, 0.22f), 3.2f, 8f);
            var flick = fire.gameObject.AddComponent<FogoTremulo>();
            flick.target = light;
            flick.flames = flames.ToArray();
        }

        // ------------------------------------------------------------------ relógio de pêndulo e aparador (lá em cima)

        static void BuildClockAndSideboard()
        {
            var wood = kit.Mat("Salao_Mogno", new Color(0.32f, 0.12f, 0.05f), 0.45f);
            var dial = kit.Mat("Salao_Mostrador", new Color(0.9f, 0.86f, 0.74f), 0.3f);
            var brass = kit.Mat("Salao_Latao", new Color(0.85f, 0.66f, 0.28f), 0.6f, 0.9f);
            var dark = kit.Mat("Salao_Ponteiro", new Color(0.05f, 0.05f, 0.05f), 0.3f);
            var glass = kit.Mat("Salao_VidroRelogio", new Color(0.1f, 0.08f, 0.06f), 0.9f);

            // Relógio de pêndulo pendurado no assoalho (parede oeste): o mostrador ficou EMBAIXO e o pêndulo aponta para CIMA.
            var clock = RoomKit.Child(root, "Relogio_Invertido", new Vector3(-HalfW + 0.2f, 0f, -4.2f));
            clock.localRotation = Quaternion.Euler(0f, 90f, 0f); // frente para +X (dentro da sala)
            RoomKit.Box(clock, "Caixa", new Vector3(0f, H - 1.05f, 0f), new Vector3(0.55f, 2.1f, 0.35f), wood, false);
            RoomKit.Box(clock, "Janela_Pendulo", new Vector3(0f, H - 0.75f, 0.176f), new Vector3(0.36f, 1.0f, 0.01f), glass, false);
            var face = RoomKit.Prim(PrimitiveType.Cylinder, clock, "Mostrador", new Vector3(0f, H - 1.75f, 0.18f), new Vector3(0.4f, 0.01f, 0.4f), dial);
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            RoomKit.Box(clock, "Ponteiro_Horas", new Vector3(0f, H - 1.7f, 0.19f), new Vector3(0.02f, 0.11f, 0.005f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, 200f);
            RoomKit.Box(clock, "Ponteiro_Min", new Vector3(0.02f, H - 1.82f, 0.19f), new Vector3(0.015f, 0.16f, 0.005f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            var pivot = RoomKit.Child(clock, "Pendulo", new Vector3(0f, H - 1.4f, 0.12f));
            pivot.gameObject.AddComponent<NaoAssar>();
            RoomKit.Box(pivot, "Haste", new Vector3(0f, 0.38f, 0f), new Vector3(0.02f, 0.76f, 0.01f), brass, false);
            RoomKit.Prim(PrimitiveType.Cylinder, pivot, "Peso", new Vector3(0f, 0.8f, 0f), new Vector3(0.16f, 0.015f, 0.16f), brass).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pivot.gameObject.AddComponent<PenduloInvertido>();

            // Aparador na parede oeste, com um vaso de flores pendurado embaixo dele.
            var sb = RoomKit.Child(root, "Aparador_Invertido", new Vector3(-HalfW + 0.28f, 0f, 2.3f));
            RoomKit.Box(sb, "Movel", new Vector3(0f, H - 0.45f, 0f), new Vector3(0.5f, 0.9f, 1.9f), wood, false);
            RoomKit.Prim(PrimitiveType.Cylinder, sb, "Vaso", new Vector3(0f, H - 1.05f, 0.4f), new Vector3(0.16f, 0.15f, 0.16f), kit.Mat("Salao_Porcelana", new Color(0.92f, 0.92f, 0.95f), 0.6f));
            var flowers = kit.Mat("Salao_Flores", new Color(0.65f, 0.15f, 0.25f), 0.2f);
            for (int i = 0; i < 5; i++)
                RoomKit.Prim(PrimitiveType.Sphere, sb, "Flor", new Vector3(Mathf.Sin(i) * 0.08f, H - 1.32f - (i % 2) * 0.06f, 0.4f + Mathf.Cos(i) * 0.08f), Vector3.one * 0.09f, flowers);
        }

        // ------------------------------------------------------------------ quadros de ponta-cabeça

        static void BuildPaintings()
        {
            var frame = kit.Mat("Salao_Moldura", new Color(0.8f, 0.62f, 0.25f), 0.6f, 0.8f);
            var canvas = kit.Mat("Salao_TelaQuadro", new Color(0.12f, 0.1f, 0.09f), 0.2f);
            var skin = kit.Mat("Salao_Pele", new Color(0.78f, 0.62f, 0.5f), 0.2f);
            var coat = kit.Mat("Salao_Roupa", new Color(0.1f, 0.12f, 0.2f), 0.2f);
            var g = RoomKit.Child(root, "Quadros_Invertidos", Vector3.zero);

            // Retratos: a cabeça está EMBAIXO e os ombros em cima.
            foreach (var (pos, yaw) in new[]
            {
                (new Vector3(-2.6f, 2.5f, -HalfD + 0.04f), 0f), (new Vector3(2.6f, 2.5f, -HalfD + 0.04f), 0f),
                (new Vector3(-HalfW + 0.04f, 2.5f, -1.2f), 90f),
            })
            {
                var p = RoomKit.Child(g, "Retrato", pos);
                p.localRotation = Quaternion.Euler(0f, yaw, 0f);
                RoomKit.Box(p, "Moldura", Vector3.zero, new Vector3(0.9f, 1.2f, 0.05f), frame, false);
                RoomKit.Box(p, "Tela", new Vector3(0f, 0f, 0.02f), new Vector3(0.74f, 1.04f, 0.02f), canvas, false);
                RoomKit.Prim(PrimitiveType.Sphere, p, "Rosto", new Vector3(0f, -0.15f, 0.035f), new Vector3(0.26f, 0.32f, 0.02f), skin);
                RoomKit.Prim(PrimitiveType.Sphere, p, "Ombros", new Vector3(0f, 0.32f, 0.035f), new Vector3(0.62f, 0.38f, 0.02f), coat);
            }
        }

        // ------------------------------------------------------------------ janelas com o céu embaixo

        static Texture2D InvertedSkyTexture()
        {
            string path = $"{GenFolder}/CeuInvertido.png";
            const int w = 256, h = 256;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, true);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float v = y / (h - 1f); // 0 = embaixo, 1 = em cima
                    Color c;
                    if (v < 0.55f) // céu embaixo: mais escuro no "alto" (que é embaixo)
                    {
                        float k = v / 0.55f;
                        c = Color.Lerp(new Color(0.22f, 0.45f, 0.85f), new Color(0.72f, 0.84f, 0.96f), k);
                        float cloud = Mathf.PerlinNoise(x * 0.025f + 4f, y * 0.06f + 9f);
                        c = Color.Lerp(c, Color.white, Mathf.Clamp01((cloud - 0.6f) * 5f) * (1f - k * 0.6f));
                    }
                    else // chão em cima: grama e árvores escuras de ponta-cabeça
                    {
                        float k = (v - 0.55f) / 0.45f;
                        c = Color.Lerp(new Color(0.24f, 0.4f, 0.18f), new Color(0.1f, 0.18f, 0.08f), k);
                        float tree = Mathf.PerlinNoise(x * 0.05f, 3f);
                        if (k < tree * 0.35f) c = Color.Lerp(c, new Color(0.08f, 0.16f, 0.07f), 0.7f);
                    }
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void BuildWindows()
        {
            var frame = kit.Mat("Salao_Caixilho", new Color(0.86f, 0.83f, 0.76f), 0.3f);
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/Materials/Salao_Janela.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(skyMat, $"{Folder}/Materials/Salao_Janela.mat");
            }
            skyMat.SetTexture("_BaseMap", InvertedSkyTexture());
            skyMat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(skyMat);

            var g = RoomKit.Child(root, "Janelas", Vector3.zero);
            var windows = new[]
            {
                (new Vector3(HalfW - 0.02f, 1.9f, -3.6f), -90f), (new Vector3(HalfW - 0.02f, 1.9f, -0.4f), -90f),
                (new Vector3(-HalfW + 0.02f, 1.9f, 0.1f), 90f),
            };
            foreach (var (pos, yaw) in windows)
            {
                var wnd = RoomKit.Child(g, "Janela", pos);
                wnd.localRotation = Quaternion.Euler(0f, yaw, 0f); // +Z local para dentro da sala
                var view = RoomKit.Prim(PrimitiveType.Quad, wnd, "Vista", new Vector3(0f, 0f, 0.005f), new Vector3(1.2f, 1.8f, 1f), skyMat);
                view.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                view.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                foreach (var (n, p, s) in new[]
                {
                    ("Topo", new Vector3(0f, 0.95f, 0.04f), new Vector3(1.36f, 0.1f, 0.08f)), ("Base", new Vector3(0f, -0.95f, 0.04f), new Vector3(1.36f, 0.1f, 0.08f)),
                    ("Esq", new Vector3(-0.63f, 0f, 0.04f), new Vector3(0.1f, 1.9f, 0.08f)), ("Dir", new Vector3(0.63f, 0f, 0.04f), new Vector3(0.1f, 1.9f, 0.08f)),
                    ("Travessa", new Vector3(0f, 0f, 0.04f), new Vector3(1.2f, 0.05f, 0.05f)), ("Montante", new Vector3(0f, 0f, 0.04f), new Vector3(0.05f, 1.8f, 0.05f)),
                })
                    RoomKit.Box(wnd, n, p, s, frame, false);
                // Luz fria entrando "de baixo para cima" (o céu está embaixo).
                RoomKit.PointLight(wnd, "Luz_Janela", new Vector3(0f, -0.8f, 0.7f), new Color(0.7f, 0.82f, 1f), 2.2f, 6.5f);
            }
        }

        // ------------------------------------------------------------------ arandelas invertidas (suporte em cima, vela pendurada)

        static void BuildSconces()
        {
            var brass = kit.Mat("Salao_Latao", new Color(0.85f, 0.66f, 0.28f), 0.6f, 0.9f);
            var wax = kit.Mat("Salao_Cera", new Color(0.93f, 0.9f, 0.8f), 0.2f);
            var flame = kit.Emissive("Salao_Chama", new Color(1f, 0.72f, 0.35f), 3f);
            var g = RoomKit.Child(root, "Arandelas", Vector3.zero);
            var spots = new[]
            {
                (new Vector3(-HalfW + 0.1f, 3.1f, -2.6f), 90f), (new Vector3(-HalfW + 0.1f, 3.1f, 4.6f), 90f),
                (new Vector3(HalfW - 0.1f, 3.1f, -2f), -90f), (new Vector3(HalfW - 0.1f, 3.1f, 5.4f), -90f),
            };
            foreach (var (pos, yaw) in spots)
            {
                var s = RoomKit.Child(g, "Arandela", pos);
                s.localRotation = Quaternion.Euler(0f, yaw, 0f);
                RoomKit.Box(s, "Suporte", new Vector3(0f, 0.12f, 0.08f), new Vector3(0.06f, 0.24f, 0.16f), brass, false);
                RoomKit.Prim(PrimitiveType.Cylinder, s, "Vela", new Vector3(0f, -0.08f, 0.16f), new Vector3(0.05f, 0.1f, 0.05f), wax);
                RoomKit.Prim(PrimitiveType.Sphere, s, "Chama", new Vector3(0f, -0.22f, 0.16f), new Vector3(0.04f, 0.08f, 0.04f), flame);
                RoomKit.PointLight(s, "Luz", new Vector3(0f, -0.3f, 0.4f), Warm, 2f, 6f);
            }
        }

        // ------------------------------------------------------------------ lustre + mecanismo (Manivela → queda → Livro Antigo)

        static void BuildChandelierPuzzle()
        {
            var gold = kit.Mat("Salao_Dourado", new Color(0.88f, 0.68f, 0.28f), 0.7f, 1f);
            var crystal = kit.Emissive("Salao_Cristal", new Color(0.85f, 0.92f, 1f), 0.6f);
            var wax = kit.Mat("Salao_Cera", new Color(0.93f, 0.9f, 0.8f), 0.2f);
            var iron = kit.Mat("Salao_Ferro", new Color(0.25f, 0.25f, 0.27f), 0.5f, 0.85f);
            var chainMat = kit.Mat("Salao_Corrente", new Color(0.55f, 0.56f, 0.6f), 0.6f, 0.9f);
            var bookMat = kit.Mat("Salao_Livro", new Color(0.35f, 0.08f, 0.08f), 0.3f);
            var g = RoomKit.Child(root, "Lustre_Puzzle", LustrePos);

            // Lustre inteiro: pivô no ponto mais baixo (para parar ao tocar o chão).
            const float bottom = H - 1.45f;
            var intact = RoomKit.Child(g, "Lustre", new Vector3(0f, bottom, 0f));
            intact.gameObject.AddComponent<NaoAssar>();
            RoomKit.Prim(PrimitiveType.Cylinder, intact, "Haste", new Vector3(0f, 1.1f, 0f), new Vector3(0.06f, 0.35f, 0.06f), gold);
            RoomKit.Prim(PrimitiveType.Cylinder, intact, "Aro", new Vector3(0f, 0.72f, 0f), new Vector3(1.7f, 0.04f, 1.7f), gold);
            RoomKit.Prim(PrimitiveType.Sphere, intact, "Bola", new Vector3(0f, 0.2f, 0f), Vector3.one * 0.3f, gold);
            RoomKit.Prim(PrimitiveType.Cylinder, intact, "Ponta", new Vector3(0f, 0.04f, 0f), new Vector3(0.05f, 0.08f, 0.05f), gold);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var p = new Vector3(Mathf.Cos(a) * 0.8f, 0f, Mathf.Sin(a) * 0.8f);
                RoomKit.Prim(PrimitiveType.Cylinder, intact, "Vela", p + new Vector3(0f, 0.58f, 0f), new Vector3(0.05f, 0.1f, 0.05f), wax);
                for (int k = 0; k < 2; k++)
                {
                    var q = new Vector3(Mathf.Cos(a + 0.4f) * (0.55f + k * 0.15f), 0.45f - k * 0.12f, Mathf.Sin(a + 0.4f) * (0.55f + k * 0.15f));
                    RoomKit.Box(intact, "Cristal", q, new Vector3(0.04f, 0.16f, 0.04f), crystal, false);
                }
            }
            // Correntes tensionadas do aro até o mecanismo na parede leste.
            var chains = new List<GameObject>();
            foreach (float dz in new[] { -0.35f, 0.35f })
            {
                var a = new Vector3(0.85f, H - 0.73f, dz);
                var b = MechPos - LustrePos + new Vector3(-0.1f, 0.05f, dz * 0.2f);
                var mid = (a + b) / 2f;
                var ch = RoomKit.Box(g, "Corrente", mid, new Vector3(0.035f, 0.035f, Vector3.Distance(a, b)), chainMat, false);
                ch.transform.localRotation = Quaternion.LookRotation(b - a);
                ch.AddComponent<NaoAssar>();
                chains.Add(ch);
            }

            // Destroços no chão (aparecem depois da queda), com o Livro Antigo no meio.
            var broken = RoomKit.Child(g, "Destrocos", Vector3.zero);
            broken.gameObject.AddComponent<NaoAssar>();
            var rnd = new System.Random(5);
            float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                var arc = RoomKit.Box(broken, "Pedaco_Aro", new Vector3(Mathf.Cos(a) * 0.75f, 0.04f, Mathf.Sin(a) * 0.75f), new Vector3(0.7f, 0.05f, 0.07f), gold, false);
                arc.transform.localRotation = Quaternion.Euler(R(-12f, 12f), -a * Mathf.Rad2Deg + 90f + R(-15f, 15f), R(-8f, 8f));
            }
            for (int i = 0; i < 26; i++)
            {
                var c = RoomKit.Box(broken, "Caco", new Vector3(R(-1.4f, 1.4f), 0.02f, R(-1.4f, 1.4f)), new Vector3(0.04f, 0.04f, R(0.06f, 0.16f)), crystal, false);
                c.transform.localRotation = Quaternion.Euler(R(0f, 90f), R(0f, 360f), R(0f, 90f));
            }
            for (int i = 0; i < 5; i++)
                RoomKit.Prim(PrimitiveType.Cylinder, broken, "Vela_Caida", new Vector3(R(-1f, 1f), 0.05f, R(-1f, 1f)), new Vector3(0.05f, 0.1f, 0.05f), wax)
                    .transform.localRotation = Quaternion.Euler(90f, R(0f, 360f), 0f);
            RoomKit.Prim(PrimitiveType.Sphere, broken, "Bola_Amassada", new Vector3(-0.2f, 0.12f, 0.3f), new Vector3(0.3f, 0.22f, 0.3f), gold);

            var book = RoomKit.Child(broken, "Pickup_LivroAntigo", new Vector3(0.35f, 0.035f, -0.25f));
            book.localRotation = Quaternion.Euler(0f, 28f, 0f);
            RoomKit.Box(book, "Livro", Vector3.zero, new Vector3(0.2f, 0.06f, 0.28f), bookMat, false);
            var bc = book.gameObject.AddComponent<BoxCollider>(); bc.size = new Vector3(0.36f, 0.3f, 0.42f); bc.isTrigger = true;
            var pick = book.gameObject.AddComponent<ItemPickup>(); pick.item = RoomKit.Item("LivroAntigo"); pick.saveId = "salao/livro";
            var glint = RoomKit.Prim(PrimitiveType.Quad, book, "Brilho", new Vector3(0.05f, 0.08f, 0f), Vector3.one * 0.1f, kit.Emissive("Salao_Brilho", new Color(1f, 0.97f, 0.85f), 3f));
            glint.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glint.AddComponent<Brilho>().maxScale = 0.1f;
            broken.gameObject.SetActive(false);

            // Mecanismo de engrenagens na parede leste, com encaixe quadrado.
            var mech = RoomKit.Child(root, "Mecanismo", MechPos);
            mech.localRotation = Quaternion.Euler(0f, -90f, 0f); // +Z local para dentro da sala (-X)
            RoomKit.Box(mech, "Placa", new Vector3(0f, 0f, -0.04f), new Vector3(0.7f, 0.7f, 0.06f), iron, false);
            var gear = RoomKit.Prim(PrimitiveType.Cylinder, mech, "Engrenagem", new Vector3(0f, 0f, 0.01f), new Vector3(0.5f, 0.02f, 0.5f), gold);
            gear.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                RoomKit.Box(mech, "Dente", new Vector3(Mathf.Cos(a) * 0.27f, Mathf.Sin(a) * 0.27f, 0.01f), new Vector3(0.06f, 0.06f, 0.04f), gold, false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg);
            }
            RoomKit.Box(mech, "Encaixe", new Vector3(0f, 0f, 0.035f), new Vector3(0.07f, 0.07f, 0.02f), kit.Mat("Salao_Fuligem", new Color(0.05f, 0.04f, 0.04f), 0.1f), false);

            // Manivela encaixada (aparece ao usar o item; gira em volta do eixo Z local).
            var crank = RoomKit.Child(mech, "Manivela_Encaixada", new Vector3(0f, 0f, 0.04f));
            RoomKit.Box(crank, "Eixo", new Vector3(0f, 0f, 0.14f), new Vector3(0.035f, 0.035f, 0.28f), iron, false);
            RoomKit.Box(crank, "Braco", new Vector3(0.11f, 0f, 0.28f), new Vector3(0.22f, 0.035f, 0.03f), iron, false);
            RoomKit.Prim(PrimitiveType.Cylinder, crank, "Cabo", new Vector3(0.22f, 0f, 0.34f), new Vector3(0.04f, 0.05f, 0.04f), kit.Mat("Salao_Lenha", new Color(0.18f, 0.1f, 0.05f), 0.1f))
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            crank.gameObject.AddComponent<NaoAssar>();
            crank.gameObject.SetActive(false);

            var fall = g.gameObject.AddComponent<LustreQueda>();
            fall.intact = intact;
            fall.broken = broken.gameObject;
            fall.chains = chains.ToArray();
            fall.crank = crank;
            fall.floorY = 0f;
            fall.crashSound = RoomKit.Sound3D(intact.gameObject, CrashClip(), 1f, 3f, 40f);
            fall.crankSound = RoomKit.Sound3D(mech.gameObject, CrankClip(), 0.8f, 2f, 20f);

            var area = RoomKit.Volume(mech, "Trava_Mecanismo", new Vector3(0f, 0f, 0.2f), new Vector3(0.9f, 0.9f, 0.5f), true);
            var lockComp = area.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { RoomKit.Item("ManivelaQuadrada") };
            lockComp.consumeItems = true;
            lockComp.usablePromptText = "Encaixar a manivela";
            lockComp.lockedMessage = "Um mecanismo de engrenagens com um encaixe quadrado. As correntes dele seguram o lustre lá em cima.";
            lockComp.unlockedMessage = "A manivela encaixa. Você gira... as correntes se soltam!";
            lockComp.activateOnUnlock = new[] { crank.gameObject };
            lockComp.saveId = "salao/mecanismo";
            RoomKit.SetPrompt(lockComp, "Examinar o mecanismo");
            UnityEventTools.AddPersistentListener(lockComp.onUnlocked, fall.Fall);
            UnityEventTools.AddPersistentListener(lockComp.onRestoredUnlocked, fall.SnapBroken);
        }

        /// <summary>Estrondo de metal e vidro: impacto grave + ruído que decai + "plins" de cristal.</summary>
        static AudioClip CrashClip()
        {
            var pings = new List<(float t0, float f, float amp)>();
            var r = new System.Random(9);
            for (int i = 0; i < 18; i++) pings.Add(((float)r.NextDouble() * 0.9f, 2200f + (float)r.NextDouble() * 3800f, 0.2f + (float)r.NextDouble() * 0.3f));
            return RoomKit.SynthClip($"{Folder}/Audio/lustre_quebrando.wav", 1.8f, (t, rnd) =>
            {
                float noise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 4.5f);
                float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 9f) * 1.4f;
                float glass = 0f;
                foreach (var (t0, f, amp) in pings)
                    if (t >= t0) glass += Mathf.Sin(2f * Mathf.PI * f * (t - t0)) * Mathf.Exp(-(t - t0) * 14f) * amp;
                return noise * 0.7f + thump + glass;
            }, seed: 3);
        }

        /// <summary>Catraca da manivela: estalos curtos e regulares.</summary>
        static AudioClip CrankClip()
        {
            return RoomKit.SynthClip($"{Folder}/Audio/manivela.wav", 1.2f, (t, rnd) =>
            {
                float local = t % 0.075f;
                float click = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-local * 160f);
                float tone = Mathf.Sin(2f * Mathf.PI * 900f * local) * Mathf.Exp(-local * 120f) * 0.5f;
                return click + tone;
            }, seed: 4);
        }

        // ------------------------------------------------------------------ prévias

        public static string RenderPreviews()
        {
            string dir = RoomKit.PreviewDir();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Vector3 O(float x, float y, float z) => Origin + new Vector3(x, y, z);
            using (var s = new RoomKit.Shooter(dir, "salao", O(0f, 1.65f, -HalfD + 1.2f), false))
            {
                s.Shot("1_entrada", O(0f, 1.65f, -HalfD + 1.2f), O(0f, 2.6f, 4f));
                s.Shot("2_mesa_teto", O(1.8f, 1.65f, -4f), O(0f, 4.4f, -1.5f));
                s.Shot("3_lareira", O(0.5f, 1.65f, 2.5f), O(0f, 4.2f, HalfD));
                s.Shot("4_lustre_mecanismo", O(-2.5f, 1.65f, 0.5f), O(2.5f, 2.6f, 3.2f));
                s.Shot("5_janela_relogio", O(1.5f, 1.65f, -2.5f), O(-HalfW, 2.4f, -2.5f));
                s.Shot("6_janela_leste", O(-1f, 1.65f, -2f), O(HalfW, 1.9f, -2f));

                var g = GameObject.Find("SalaoInvertido/Lustre_Puzzle").GetComponent<LustreQueda>();
                g.SnapBroken();
                s.Shot("7_lustre_caido", O(-1.2f, 1.65f, 1f), O(0f, 0f, LustrePos.z));
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[Somnium] Prévias do Salão Invertido salvas em {Path.GetFullPath(dir)}");
            return Path.GetFullPath(dir);
        }
    }
}
