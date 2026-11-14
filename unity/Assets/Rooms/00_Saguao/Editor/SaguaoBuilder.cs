using System.Collections.Generic;
using System.IO;
using System.Linq;
using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Gera o blockout do Saguão central (hall de mansão decadente):
    /// porta principal com 5 fechaduras (final do jogo), estátua do medalhão (Chave 4),
    /// escadaria dupla com mezanino, 4 portas trancadas para as outras salas, lustre e arandelas.
    /// Depois de gerada, a cena pode (e deve) ser editada à mão; gerar de novo sobrescreve.
    /// </summary>
    public static class SaguaoBuilder
    {
        const string Folder = "Assets/Rooms/00_Saguao";
        public const string ScenePath = Folder + "/saguao.unity";
        /// <summary>Id do ponto onde o jogador aparece ao voltar da Colinas XP.</summary>
        public const string SpawnFromColinas = "de_colinas";
        const int MainLocks = 4; // fluxo enxuto (opção B): 4 chaves
        const string MatFolder = Folder + "/Materials";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";
        const string ItemsFolder = "Assets/Data/Items";

        // Longe da área externa e da cena de teste, até a integração com o prédio/portais ser definida.
        static readonly Vector3 Origin = new Vector3(1000f, 0f, 0f);

        // Dimensões internas (múltiplos de 1,2 m para o xadrez do piso casar entre as placas).
        const float HalfW = 7.2f;   // x: -7,2 .. 7,2
        const float HalfD = 8.4f;   // z: -8,4 .. 8,4
        const float Height = 7f;
        const float T = 0.3f;       // espessura das paredes
        const float MainDoorHalfW = 1.4f, MainDoorH = 3.2f;
        const float SideDoorHalfW = 0.7f, SideDoorH = 2.6f, SideDoorZ = 3.2f;
        const float BalconyZ = 5.6f, BalconyTop = 3.6f;

        static int groundLayer;
        static Transform root;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        // ------------------------------------------------------------------ menus

        [MenuItem("Somnium/Salas/Abrir Saguão")]
        public static void OpenSaguao()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorUtility.DisplayDialog("Saguão", "A cena do Saguão ainda não existe. Gerar o blockout agora?", "Gerar", "Cancelar")) return;
                Build();
            }
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Debug.Log("[Somnium] Saguão aberto junto com a persistencia. Dê Play: você nasce de frente para a Porta Principal.");
        }

        [MenuItem("Somnium/Salas/Gerar blockout do Saguão (sobrescreve)")]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Saguão", "A cena do Saguão já existe. Gerar de novo APAGA as edições feitas à mão nela. Continuar?", "Gerar de novo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            OpenSaguao();
        }

        [MenuItem("Somnium/Salas/Renderizar prévias do Saguão")]
        public static void RenderPreviewsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string dir = RenderPreviews();
            EditorUtility.RevealInFinder(dir);
        }

        /// <summary>Batch: gera a cena e renderiza as prévias. Pasta de saída: argumento "-previewOut <pasta>".</summary>
        public static void BuildAndPreview()
        {
            Build();
            RenderPreviews();
        }

        /// <summary>Renderiza vistas fixas do Saguão (inclusive o final com a porta aberta) em PNG. Não altera a cena salva.</summary>
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            void Shot(string name, Vector3 pos, Vector3 lookAt)
            {
                cam.transform.position = Origin + pos;
                cam.transform.LookAt(Origin + lookAt);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"saguao_{name}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                RenderTexture.active = null;
            }

            // Aquecimento: os primeiros renders saem com artefatos enquanto os shaders compilam.
            cam.transform.position = Origin + new Vector3(0f, 1.65f, -6.8f);
            for (int i = 0; i < 3; i++) cam.Render();

            Shot("1_spawn", new Vector3(0f, 1.65f, -6.8f), new Vector3(0f, 1.9f, 8.4f));
            Shot("2_estatua", new Vector3(1.2f, 1.65f, -4.2f), new Vector3(0f, 1.1f, -1.6f));
            Shot("3_visao_geral", new Vector3(-6.6f, 6.4f, -8f), new Vector3(1.5f, 1f, 3f));
            Shot("4_mezanino", new Vector3(5.5f, 5.25f, 7.6f), new Vector3(-3f, 1f, -4f));
            Shot("5_porta_principal", new Vector3(0f, 1.65f, 4.5f), new Vector3(0f, 1.6f, 8.4f));
            Shot("6_portas_oeste", new Vector3(3f, 1.65f, 0f), new Vector3(-7.2f, 1.6f, 0f));
            Shot("8_piso_spawn", new Vector3(0f, 1.65f, -6.8f), new Vector3(0f, 0f, -4.5f));

            // Final: porta aberta e luz de fora.
            var door = GameObject.Find("Saguao/PortaPrincipal").transform;
            door.Find("Exterior_Luz").gameObject.SetActive(true);
            door.Find("Folha_Esq").localRotation = Quaternion.Euler(0f, -95f, 0f);
            door.Find("Folha_Dir").localRotation = Quaternion.Euler(0f, 95f, 0f);
            Shot("7_final", new Vector3(0f, 1.65f, 3.5f), new Vector3(0f, 1.6f, 8.4f));

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); // descarta as mudanças da prévia
            Debug.Log($"[Somnium] Prévias do Saguão salvas em {Path.GetFullPath(dir)}");
            return Path.GetFullPath(dir);
        }

        // ------------------------------------------------------------------ geração

        /// <summary>Público para rodar em batch: -executeMethod Somnium.EditorTools.Rooms.SaguaoBuilder.Build</summary>
        public static void Build()
        {
            EnsureFolder(MatFolder);
            mats.Clear();
            groundLayer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupEnvironment();

            root = new GameObject("Saguao").transform;
            root.position = Origin;
            root.gameObject.AddComponent<RoomSettings>().roomName = "Saguão";

            BuildShell();
            BuildBalconyAndStairs();
            var ending = BuildMainDoor();
            BuildStatue();
            BuildSideDoors();
            BuildLights();
            BuildDecor();

            var spawn = Child(root, "PlayerSpawn", new Vector3(0f, 0f, -6.8f));
            spawn.gameObject.AddComponent<PlayerSpawnPoint>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Blockout do Saguão salvo em {ScenePath}. Final do jogo ligado em {ending.name}.");
        }

        static void SetupEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.17f, 0.145f, 0.125f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogColor = new Color(0.05f, 0.04f, 0.035f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        // ------------------------------------------------------------------ casca: piso, paredes, teto

        static void BuildShell()
        {
            var shell = Child(root, "Estrutura", Vector3.zero);

            // Piso em placas (4 x 7): no celular cada objeto recebe no máximo 4 luzes.
            var piso = Child(shell, "Piso", Vector3.zero);
            const int nx = 4, nz = 7;
            float tw = HalfW * 2f / nx, td = HalfD * 2f / nz;
            var floorMat = CheckerMaterial("Saguao_PisoXadrez", tw, td);
            for (int ix = 0; ix < nx; ix++)
                for (int iz = 0; iz < nz; iz++)
                {
                    var p = new Vector3(-HalfW + tw * (ix + 0.5f), -0.1f, -HalfD + td * (iz + 0.5f));
                    var tile = Box(piso, $"Placa_{ix}_{iz}", p, new Vector3(tw, 0.2f, td), floorMat);
                    tile.layer = groundLayer;
                }

            var wall = Mat("Saguao_Parede", new Color(0.27f, 0.12f, 0.10f), 0.15f);
            var trim = Mat("Saguao_Madeira", new Color(0.22f, 0.13f, 0.07f), 0.35f);
            var paredes = Child(shell, "Paredes", Vector3.zero);
            float zN = HalfD + T / 2f, zS = -HalfD - T / 2f, xW = -HalfW - T / 2f, xE = HalfW + T / 2f;

            // Norte: abertura da porta principal.
            WallX(paredes, "Norte_Esq", zN, -HalfW - T, -MainDoorHalfW, 0f, Height, wall);
            WallX(paredes, "Norte_Dir", zN, MainDoorHalfW, HalfW + T, 0f, Height, wall);
            WallX(paredes, "Norte_Verga", zN, -MainDoorHalfW, MainDoorHalfW, MainDoorH, Height, wall);
            // Sul: inteira.
            WallX(paredes, "Sul", zS, -HalfW - T, HalfW + T, 0f, Height, wall);
            // Oeste e Leste: duas portas cada.
            foreach (var (side, x) in new[] { ("Oeste", xW), ("Leste", xE) })
            {
                float a = SideDoorZ - SideDoorHalfW, b = SideDoorZ + SideDoorHalfW;
                WallZ(paredes, $"{side}_A", x, -HalfD, -b, 0f, Height, wall);
                WallZ(paredes, $"{side}_B", x, -a, a, 0f, Height, wall);
                WallZ(paredes, $"{side}_C", x, b, HalfD, 0f, Height, wall);
                WallZ(paredes, $"{side}_VergaSul", x, -b, -a, SideDoorH, Height, wall);
                WallZ(paredes, $"{side}_VergaNorte", x, a, b, SideDoorH, Height, wall);
            }

            // Rodapé de madeira (lambri baixo) nas paredes sul, leste e oeste.
            var lambri = Child(shell, "Lambri", Vector3.zero);
            Box(lambri, "Sul", new Vector3(0f, 0.6f, -HalfD + 0.03f), new Vector3(HalfW * 2f, 1.2f, 0.06f), trim);

            Box(shell, "Teto", new Vector3(0f, Height + 0.15f, 0f), new Vector3(HalfW * 2f + 2 * T, 0.3f, HalfD * 2f + 2 * T),
                Mat("Saguao_Teto", new Color(0.16f, 0.13f, 0.11f), 0.1f));
        }

        // ------------------------------------------------------------------ mezanino e escadaria dupla

        static void BuildBalconyAndStairs()
        {
            var wood = Mat("Saguao_Madeira", new Color(0.22f, 0.13f, 0.07f), 0.35f);
            var carpet = Mat("Saguao_Tapete", new Color(0.35f, 0.04f, 0.05f), 0.05f);
            var g = Child(root, "Mezanino", Vector3.zero);

            float depth = HalfD - BalconyZ;
            var slab = Box(g, "Piso_Mezanino", new Vector3(0f, BalconyTop - 0.15f, BalconyZ + depth / 2f), new Vector3(HalfW * 2f, 0.3f, depth), wood);
            slab.layer = groundLayer;

            // Guarda-corpo, com aberturas onde as escadas chegam.
            float railY = BalconyTop + 0.5f;
            foreach (var (name, x0, x1) in new[] { ("Esq", -HalfW, -3.2f), ("Centro", -1.8f, 1.8f), ("Dir", 3.2f, HalfW) })
                Box(g, $"GuardaCorpo_{name}", new Vector3((x0 + x1) / 2f, railY, BalconyZ + 0.05f), new Vector3(x1 - x0, 1f, 0.1f), wood);

            // Duas escadas (degraus maciços de 20 cm), de z = 1,1 até o mezanino.
            const int steps = 18;
            float run = (BalconyZ - 1.1f) / steps, rise = BalconyTop / steps;
            foreach (var (name, xc) in new[] { ("Esq", -2.5f), ("Dir", 2.5f) })
            {
                var stair = Child(g, $"Escada_{name}", Vector3.zero);
                for (int i = 0; i < steps; i++)
                {
                    float top = rise * (i + 1);
                    var step = Box(stair, $"Degrau_{i:00}", new Vector3(xc, top / 2f, 1.1f + run * (i + 0.5f)), new Vector3(1.4f, top, run), carpet);
                    step.layer = groundLayer;
                    // Os degraus são só visuais (exceto o último, que emenda com o mezanino):
                    // quem dá a subida é a rampa invisível abaixo, sem os trancos de degrau em degrau.
                    if (i < steps - 1) Object.DestroyImmediate(step.GetComponent<Collider>());
                }

                // Rampa invisível passando pelas quinas dos degraus: do chão (z = 1,1 - run) até o topo
                // do penúltimo degrau (z = BalconyZ - run, y = BalconyTop). Inclinação ~38,7° (< slopeLimit 45°).
                float z0 = 1.1f - run, z1 = BalconyZ - run;
                float rampLen = Mathf.Sqrt((z1 - z0) * (z1 - z0) + BalconyTop * BalconyTop);
                float rampAng = Mathf.Atan2(BalconyTop, z1 - z0) * Mathf.Rad2Deg;
                const float rampThick = 0.1f;
                var ramp = new GameObject("Rampa_Colisao");
                ramp.transform.SetParent(stair, false);
                ramp.layer = groundLayer;
                // Centro deslocado meia espessura para baixo, perpendicular à rampa, para o topo ficar na linha das quinas.
                var mid = new Vector3(xc, BalconyTop / 2f, (z0 + z1) / 2f);
                var down = Quaternion.Euler(-rampAng, 0f, 0f) * Vector3.down;
                ramp.transform.localPosition = mid + down * (rampThick / 2f);
                ramp.transform.localRotation = Quaternion.Euler(-rampAng, 0f, 0f);
                ramp.AddComponent<BoxCollider>().size = new Vector3(1.4f, rampThick, rampLen);
                // Corrimãos laterais (bloqueiam a queda pelos lados).
                float len = Mathf.Sqrt((BalconyZ - 1.1f) * (BalconyZ - 1.1f) + BalconyTop * BalconyTop);
                float ang = Mathf.Atan2(BalconyTop, BalconyZ - 1.1f) * Mathf.Rad2Deg;
                foreach (float side in new[] { -0.75f, 0.75f })
                {
                    var rail = Box(stair, side < 0 ? "Corrimao_A" : "Corrimao_B",
                                   new Vector3(xc + side, BalconyTop / 2f + 0.55f, (1.1f + BalconyZ) / 2f), new Vector3(0.08f, 1.1f, len), wood);
                    rail.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
                }
            }
        }

        // ------------------------------------------------------------------ porta principal (5 fechaduras → final)

        static GameObject BuildMainDoor()
        {
            var wood = Mat("Saguao_PortaPrincipal", new Color(0.18f, 0.09f, 0.04f), 0.4f);
            var iron = Mat("Saguao_Ferro", new Color(0.12f, 0.12f, 0.13f), 0.5f, 0.8f);
            var gold = Mat("Saguao_Dourado", new Color(0.85f, 0.65f, 0.25f), 0.7f, 1f);
            var g = Child(root, "PortaPrincipal", Vector3.zero);
            float z = HalfD + 0.06f;

            // Batente.
            Box(g, "Batente_Esq", new Vector3(-MainDoorHalfW - 0.1f, MainDoorH / 2f, HalfD - 0.02f), new Vector3(0.2f, MainDoorH, 0.12f), wood);
            Box(g, "Batente_Dir", new Vector3(MainDoorHalfW + 0.1f, MainDoorH / 2f, HalfD - 0.02f), new Vector3(0.2f, MainDoorH, 0.12f), wood);
            Box(g, "Batente_Topo", new Vector3(0f, MainDoorH + 0.1f, HalfD - 0.02f), new Vector3(MainDoorHalfW * 2f + 0.4f, 0.2f, 0.12f), wood);

            // Folhas com pivô na dobradiça; abrem para fora (norte). Só abrem pelo final do jogo.
            var doors = new List<Itr_Door>();
            foreach (var (name, hingeX, dir, angle) in new[] { ("Folha_Esq", -MainDoorHalfW, 1f, -95f), ("Folha_Dir", MainDoorHalfW, -1f, 95f) })
            {
                var pivot = Child(g, name, new Vector3(hingeX, 0f, z));
                Box(pivot, "Madeira", new Vector3(dir * MainDoorHalfW / 2f, MainDoorH / 2f, 0f), new Vector3(MainDoorHalfW - 0.02f, MainDoorH - 0.02f, 0.1f), wood);
                var door = pivot.gameObject.AddComponent<Itr_Door>();
                door.openAngle = angle;
                door.openSpeed = 1.2f;
                door.interactableByPlayer = false;
                doors.Add(door);
            }

            // Coluna com as fechaduras na borda da folha esquerda (gira junto) + as chaves que aparecem ao inserir.
            var locks = Child(doors[0].transform, "Fechaduras", new Vector3(MainDoorHalfW - 0.14f, 0f, 0f));
            var inserted = new List<GameObject>();
            for (int i = 0; i < MainLocks; i++)
            {
                float y = 1.0f + i * 0.32f;
                var plate = Box(locks, $"Fechadura_{i + 1}", new Vector3(0f, y, -0.07f), new Vector3(0.14f, 0.18f, 0.04f), iron);
                Object.DestroyImmediate(plate.GetComponent<Collider>());
                var key = Box(locks, $"ChaveInserida_{i + 1}", new Vector3(0f, y, -0.12f), new Vector3(0.05f, 0.12f, 0.08f), gold);
                Object.DestroyImmediate(key.GetComponent<Collider>());
                key.SetActive(false);
                inserted.Add(key);
            }

            // Área clicável: cobre a frente da porta (trigger), para a mira achar a fechadura.
            var area = Box(g, "Trava_PortaPrincipal", new Vector3(0f, MainDoorH / 2f, HalfD - 0.15f), new Vector3(MainDoorHalfW * 2f, MainDoorH, 0.1f), null);
            Object.DestroyImmediate(area.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(area.GetComponent<MeshFilter>());
            area.GetComponent<BoxCollider>().isTrigger = true;
            var lockComp = area.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { Item("Chave1"), Item("Chave2"), Item("Chave3"), Item("Chave4") };
            lockComp.acceptPartial = true;
            lockComp.consumeItems = true;
            lockComp.lockedMessage = "A porta principal tem quatro fechaduras.";
            lockComp.unlockedMessage = "A última chave gira. A porta se abre...";
            lockComp.saveId = "saguao/porta_principal";
            SetPrompt(lockComp, "Examinar a porta principal");

            var indicator = locks.gameObject.AddComponent<LockProgressIndicator>();
            indicator.target = lockComp;
            indicator.insertedVisuals = inserted.ToArray();

            // Lá fora: luz forte (ligada só no final).
            var exterior = Child(g, "Exterior_Luz", new Vector3(0f, 0f, HalfD + 2.5f));
            var glow = Box(exterior, "Brilho", new Vector3(0f, 3f, 1.5f), new Vector3(10f, 8f, 0.1f),
                           Emissive("Saguao_LuzExterior", new Color(1f, 0.97f, 0.9f), 6f));
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            var outsideFloor = Box(exterior, "Chao", new Vector3(0f, -0.05f, 0f), new Vector3(6f, 0.1f, 5f),
                                   Emissive("Saguao_ChaoExterior", new Color(0.95f, 0.93f, 0.85f), 1.5f));
            Object.DestroyImmediate(outsideFloor.GetComponent<Collider>());
            var light = new GameObject("Luz").AddComponent<Light>();
            light.transform.SetParent(exterior, false);
            light.transform.localPosition = new Vector3(0f, 2f, -1.5f);
            light.type = LightType.Point;
            light.range = 18f;
            light.intensity = 8f;
            light.color = new Color(1f, 0.97f, 0.9f);
            exterior.gameObject.SetActive(false);

            var ending = g.gameObject.AddComponent<GameEnding>();
            ending.doorsToOpen = doors.ToArray();
            ending.activateOnBegin = new[] { exterior.gameObject };
            ending.message = "Você acordou.";
            UnityEventTools.AddPersistentListener(lockComp.onUnlocked, ending.Begin);
            return g.gameObject;
        }

        // ------------------------------------------------------------------ estátua do medalhão (Chave 4)

        static void BuildStatue()
        {
            var stone = Mat("Saguao_Pedra", new Color(0.55f, 0.54f, 0.5f), 0.2f);
            var dark = Mat("Saguao_PedraEscura", new Color(0.18f, 0.17f, 0.16f), 0.2f);
            var gold = Mat("Saguao_Dourado", new Color(0.85f, 0.65f, 0.25f), 0.7f, 1f);

            var g = Child(root, "Estatua", new Vector3(0f, 0f, -1.6f));
            Box(g, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 1f, 1.2f), stone);
            Box(g, "Figura", new Vector3(0f, 1.9f, 0.15f), new Vector3(0.55f, 1.8f, 0.45f), stone);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Place(head, g, "Cabeca", new Vector3(0f, 3.0f, 0.15f), Vector3.one * 0.4f, stone);
            Box(g, "Braco_Esq", new Vector3(-0.38f, 2.2f, -0.05f), new Vector3(0.16f, 0.16f, 0.6f), stone);
            Box(g, "Braco_Dir", new Vector3(0.38f, 2.2f, -0.05f), new Vector3(0.16f, 0.16f, 0.6f), stone);

            // Encaixe redondo com sol gravado na frente do pedestal.
            var socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(socket, g, "Encaixe", new Vector3(0f, 0.6f, -0.61f), new Vector3(0.3f, 0.015f, 0.3f), dark);
            socket.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var medal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(medal, g, "MedalhaoEncaixado", new Vector3(0f, 0.6f, -0.625f), new Vector3(0.26f, 0.015f, 0.26f), gold);
            medal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(medal.GetComponent<Collider>());
            medal.SetActive(false);

            // Tampa no alto do pedestal: some ao encaixar o medalhão e revela a Chave 4.
            var lid = Box(g, "Tampa", new Vector3(0f, 1.04f, -0.38f), new Vector3(0.45f, 0.08f, 0.3f), dark);
            var key = Box(g, "Pickup_Chave4", new Vector3(0f, 1.03f, -0.38f), new Vector3(0.18f, 0.03f, 0.06f), gold);
            key.GetComponent<BoxCollider>().size = new Vector3(2f, 6f, 4f); // alvo maior para a mira
            var pickup = key.AddComponent<ItemPickup>();
            pickup.item = Item("Chave4");
            pickup.saveId = "saguao/chave4";
            key.SetActive(false);

            var lockComp = g.gameObject.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { Item("MedalhaoDoSol") };
            lockComp.lockedMessage = "Uma estátua sem rosto. No pedestal, um encaixe redondo com um sol gravado em volta.";
            lockComp.unlockedMessage = "O medalhão encaixa. Uma tampa se abre no alto do pedestal.";
            lockComp.deactivateOnUnlock = new[] { lid };
            lockComp.activateOnUnlock = new[] { medal, key };
            lockComp.saveId = "saguao/estatua";
            SetPrompt(lockComp, "Examinar a estátua");
        }

        // ------------------------------------------------------------------ portas para as outras salas

        static void BuildSideDoors()
        {
            var wood = Mat("Saguao_Madeira", new Color(0.22f, 0.13f, 0.07f), 0.35f);
            var door = Mat("Saguao_PortaSala", new Color(0.3f, 0.17f, 0.08f), 0.35f);
            var board = Mat("Saguao_Placa", new Color(0.08f, 0.06f, 0.05f), 0.3f);
            var g = Child(root, "PortasSalas", Vector3.zero);

            var doors = new[]
            {
                ("ColinasXP", "Colinas XP", -1f, SideDoorZ),
                ("EscritorioAzul", "Escritório Azul", -1f, -SideDoorZ),
                ("SalaoInvertido", "Salão Invertido", 1f, SideDoorZ),
                ("Estufa", "Estufa", 1f, -SideDoorZ),
            };

            foreach (var (id, label, side, z) in doors)
            {
                float x = side * HalfW;
                var d = Child(g, $"Porta_{id}", new Vector3(x, 0f, z));
                d.localRotation = Quaternion.Euler(0f, side < 0 ? -90f : 90f, 0f); // +Z local aponta para dentro da parede

                Box(d, "Batente_A", new Vector3(-SideDoorHalfW - 0.07f, SideDoorH / 2f, -0.02f), new Vector3(0.14f, SideDoorH, 0.1f), wood);
                Box(d, "Batente_B", new Vector3(SideDoorHalfW + 0.07f, SideDoorH / 2f, -0.02f), new Vector3(0.14f, SideDoorH, 0.1f), wood);
                Box(d, "Batente_Topo", new Vector3(0f, SideDoorH + 0.07f, -0.02f), new Vector3(SideDoorHalfW * 2f + 0.28f, 0.14f, 0.1f), wood);
                var leaf = Box(d, "Folha", new Vector3(0f, SideDoorH / 2f, 0.1f), new Vector3(SideDoorHalfW * 2f, SideDoorH, 0.08f), door);
                if (id == "ColinasXP")
                {
                    var roomDoor = leaf.AddComponent<RoomDoor>();
                    roomDoor.targetScenePath = ColinasBuilder.ScenePath;
                    roomDoor.spawnId = "inicio";
                    SetPrompt(roomDoor, $"Entrar: {label}");

                    // Ponto de chegada ao voltar: na frente da porta, virado para dentro do hall.
                    var back = Child(d, "Spawn_DeColinas", new Vector3(0f, 0f, -1.3f));
                    back.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    var sp = back.gameObject.AddComponent<PlayerSpawnPoint>();
                    sp.id = SpawnFromColinas;
                    sp.placeOnStart = false;
                }
                else
                {
                    var msg = leaf.AddComponent<MessageInteractable>();
                    msg.message = $"A porta para {label} está trancada.\n(Sala ainda em construção.)";
                    SetPrompt(msg, $"Abrir: {label}");
                }

                // Placa com o nome da sala.
                var sign = Box(d, "Placa", new Vector3(0f, SideDoorH + 0.45f, -0.03f), new Vector3(1.3f, 0.3f, 0.04f), board);
                Object.DestroyImmediate(sign.GetComponent<Collider>());
                AddSignText(d, label, new Vector3(0f, SideDoorH + 0.45f, -0.06f));
            }
        }

        static void AddSignText(Transform parent, string text, Vector3 localPos)
        {
            var go = new GameObject("PlacaTexto", typeof(RectTransform), typeof(Canvas));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localPosition = localPos;
            rt.localScale = Vector3.one * 0.001f; // 1 px = 1 mm
            rt.sizeDelta = new Vector2(1250f, 280f);
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var trt = t.rectTransform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 150;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.85f, 0.7f, 0.4f);
            t.raycastTarget = false;
            t.text = text;
        }

        // ------------------------------------------------------------------ luz

        static void BuildLights()
        {
            var brass = Mat("Saguao_Dourado", new Color(0.85f, 0.65f, 0.25f), 0.7f, 1f);
            var candle = Emissive("Saguao_Vela", new Color(1f, 0.75f, 0.45f), 2.5f);
            var g = Child(root, "Iluminacao", Vector3.zero);
            var warm = new Color(1f, 0.78f, 0.52f);

            // Lustre central.
            var lustre = Child(g, "Lustre", new Vector3(0f, 0f, -0.5f));
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(rod, lustre, "Haste", new Vector3(0f, 6.2f, 0f), new Vector3(0.05f, 0.8f, 0.05f), brass);
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(ring, lustre, "Aro", new Vector3(0f, 5.3f, 0f), new Vector3(1.8f, 0.06f, 1.8f), brass);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Place(c, lustre, $"Vela_{i}", new Vector3(Mathf.Cos(a) * 0.85f, 5.45f, Mathf.Sin(a) * 0.85f), new Vector3(0.06f, 0.1f, 0.06f), candle);
                Object.DestroyImmediate(c.GetComponent<Collider>());
            }
            foreach (var col in lustre.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            PointLight(lustre, "Luz_Lustre", new Vector3(0f, 4.7f, 0f), warm, 7f, 20f, LightShadows.Soft);

            // Arandelas: paredes laterais (entre as portas), ao lado da porta principal e na parede sul (perto do spawn).
            var sconces = new[]
            {
                ("Arandela_Oeste", new Vector3(-HalfW + 0.12f, 2.5f, 0f)),
                ("Arandela_Leste", new Vector3(HalfW - 0.12f, 2.5f, 0f)),
                ("Arandela_PortaEsq", new Vector3(-2.2f, 2.4f, HalfD - 0.12f)),
                ("Arandela_PortaDir", new Vector3(2.2f, 2.4f, HalfD - 0.12f)),
                ("Arandela_SulEsq", new Vector3(-2.6f, 2.6f, -HalfD + 0.12f)),
                ("Arandela_SulDir", new Vector3(2.6f, 2.6f, -HalfD + 0.12f)),
            };
            foreach (var (name, pos) in sconces)
            {
                var s = Child(g, name, pos);
                var body = Box(s, "Suporte", Vector3.zero, new Vector3(0.12f, 0.25f, 0.12f), brass);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                var flame = Box(s, "Chama", new Vector3(0f, 0.18f, 0f), new Vector3(0.05f, 0.1f, 0.05f), candle);
                Object.DestroyImmediate(flame.GetComponent<Collider>());
                Vector3 inward = -new Vector3(Mathf.Sign(pos.x) * (Mathf.Abs(pos.x) > HalfW - 1 ? 1 : 0), 0f,
                                              Mathf.Sign(pos.z) * (Mathf.Abs(pos.z) > HalfD - 1 ? 1 : 0)) * 0.35f;
                PointLight(s, "Luz", inward + new Vector3(0f, 0.2f, 0f), warm, 2.2f, 7f, LightShadows.None);
            }
        }

        // ------------------------------------------------------------------ decoração (escala e clima)

        static void BuildDecor()
        {
            var g = Child(root, "Decoracao", Vector3.zero);
            var rug = Mat("Saguao_Tapete", new Color(0.35f, 0.04f, 0.05f), 0.05f);
            var wood = Mat("Saguao_Madeira", new Color(0.22f, 0.13f, 0.07f), 0.35f);
            var frame = Mat("Saguao_Dourado", new Color(0.85f, 0.65f, 0.25f), 0.7f, 1f);
            var canvasMat = Mat("Saguao_Quadro", new Color(0.1f, 0.12f, 0.1f), 0.3f);
            var pot = Mat("Saguao_Vaso", new Color(0.4f, 0.38f, 0.33f), 0.3f);
            var plant = Mat("Saguao_PlantaSeca", new Color(0.28f, 0.24f, 0.12f), 0.1f);

            var r = Box(g, "Tapete", new Vector3(0f, 0.005f, -1.8f), new Vector3(4f, 0.01f, 6.5f), rug);
            Object.DestroyImmediate(r.GetComponent<Collider>());

            // Bancos na parede sul.
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                var b = Child(g, x < 0 ? "Banco_Esq" : "Banco_Dir", new Vector3(x, 0f, -HalfD + 0.45f));
                Box(b, "Assento", new Vector3(0f, 0.45f, 0f), new Vector3(2f, 0.1f, 0.5f), wood);
                Box(b, "Encosto", new Vector3(0f, 0.85f, 0.22f), new Vector3(2f, 0.7f, 0.08f), wood);
                Box(b, "Pe_A", new Vector3(-0.9f, 0.2f, 0f), new Vector3(0.1f, 0.4f, 0.45f), wood);
                Box(b, "Pe_B", new Vector3(0.9f, 0.2f, 0f), new Vector3(0.1f, 0.4f, 0.45f), wood);
            }

            // Quadros: grande na parede sul e no mezanino.
            Painting(g, "Quadro_Sul", new Vector3(0f, 3.6f, -HalfD + 0.06f), new Vector3(3.2f, 2.2f, 0.06f), 0f, frame, canvasMat);
            Painting(g, "Quadro_Mezanino", new Vector3(0f, 5.4f, HalfD - 0.06f), new Vector3(2.6f, 1.8f, 0.06f), 180f, frame, canvasMat);

            // Vasos com plantas secas nos cantos.
            foreach (var p in new[] { new Vector3(-6.6f, 0f, -7.8f), new Vector3(6.6f, 0f, -7.8f), new Vector3(-6.6f, 0f, 5f), new Vector3(6.6f, 0f, 5f) })
            {
                var v = Child(g, "Vaso", p);
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Place(c, v, "Vaso", new Vector3(0f, 0.35f, 0f), new Vector3(0.6f, 0.35f, 0.6f), pot);
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Place(s, v, "Planta", new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 0.9f, 0.8f), plant);
                Object.DestroyImmediate(s.GetComponent<Collider>());
            }

            // Bilhete no banco: dá o tom e lembra o objetivo.
            var note = Box(g, "Bilhete", new Vector3(-4.2f, 0.505f, -HalfD + 0.45f), new Vector3(0.21f, 0.005f, 0.3f), Mat("Saguao_Papel", new Color(0.85f, 0.82f, 0.7f), 0.1f));
            note.GetComponent<BoxCollider>().size = new Vector3(1f, 30f, 1f);
            var msg = note.AddComponent<MessageInteractable>();
            msg.message = "\"Quatro portas, quatro chaves. Quem sonha não sai pela porta da frente sem todas elas.\"";
            msg.duration = 6f;
            SetPrompt(msg, "Ler bilhete");
        }

        static void Painting(Transform parent, string name, Vector3 pos, Vector3 size, float yaw, Material frame, Material canvas)
        {
            var p = Child(parent, name, pos);
            p.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var f = Box(p, "Moldura", Vector3.zero, size, frame);
            Object.DestroyImmediate(f.GetComponent<Collider>());
            var c = Box(p, "Tela", new Vector3(0f, 0f, 0.02f), new Vector3(size.x - 0.2f, size.y - 0.2f, size.z), canvas);
            Object.DestroyImmediate(c.GetComponent<Collider>());
        }

        // ------------------------------------------------------------------ helpers

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

        static void WallX(Transform parent, string name, float z, float x0, float x1, float y0, float y1, Material mat) =>
            Box(parent, name, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), mat);

        static void WallZ(Transform parent, string name, float x, float z0, float z1, float y0, float y1, Material mat) =>
            Box(parent, name, new Vector3(x, (y0 + y1) / 2f, (z0 + z1) / 2f), new Vector3(T, y1 - y0, z1 - z0), mat);

        static void PointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range, LightShadows shadows)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows;
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

        static Material Emissive(string name, Color color, float intensity)
        {
            var mat = Mat(name, color, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Xadrez creme/verde-escuro com casas de 60 cm, ajustado ao tamanho da placa.</summary>
        static Material CheckerMaterial(string name, float tileW, float tileD)
        {
            string texPath = $"{MatFolder}/Xadrez.png";
            if (!File.Exists(texPath))
            {
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                var a = new Color(0.78f, 0.74f, 0.64f);
                var b = new Color(0.07f, 0.12f, 0.09f);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                        tex.SetPixel(x, y, ((x / (size / 2)) + (y / (size / 2))) % 2 == 0 ? a : b);
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var mat = Mat(name, Color.white, 0.25f);
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", new Vector2(tileW / 1.2f, tileD / 1.2f)); // período do xadrez = 1,2 m
            EditorUtility.SetDirty(mat);
            return mat;
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
