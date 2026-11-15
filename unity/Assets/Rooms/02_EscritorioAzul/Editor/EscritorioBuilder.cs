using System.Collections.Generic;
using Somnium.Ambiente;
using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Gera o blockout do Escritório Azul (arquivo liminar dos anos 50, tudo em azul):
    /// fileiras de arquivos de metal formando corredores, mesas com máquinas de escrever, luz fluorescente fria
    /// (uma piscando), o alçapão acorrentado no fim do corredor central (Alicate → nicho com Chave 2 + Lata de Ração)
    /// e o almoxarifado com o galão de ácido (Cartucho Vazio → Herbicida).
    /// Depois de gerada, a cena pode ser editada à mão; gerar de novo sobrescreve.
    /// </summary>
    public static class EscritorioBuilder
    {
        public const string Folder = "Assets/Rooms/02_EscritorioAzul";
        public const string ScenePath = Folder + "/escritorio_azul.unity";
        const string GenFolder = Folder + "/Gerado";
        const string ModelFolder = Folder + "/Models";
        const string PersistenciaPath = "Assets/Scenes/persistencia.unity";

        public static readonly Vector3 Origin = new Vector3(3000f, 0f, 0f);

        const float HalfW = 6f, HalfD = 11f, Height = 3f, T = 0.25f;
        const float DoorHalfW = 0.6f, DoorH = 2.3f;
        // Alçapão: buraco de 1 x 1 m no fim do corredor central.
        const float HatchZ0 = 8.9f, HatchZ1 = 9.9f, HatchHalf = 0.5f, PitDepth = 0.6f;
        static readonly Vector3 ClosetPos = new Vector3(4.4f, 0f, 10.62f);

        static readonly Color LightColor = new Color(0.82f, 0.9f, 1f);

        static RoomKit kit;
        static Transform root;
        static int groundLayer;

        // ------------------------------------------------------------------ menus

        [MenuItem("Somnium/Salas/Abrir Escritório Azul")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                if (!EditorUtility.DisplayDialog("Escritório Azul", "A cena ainda não existe. Gerar o blockout agora?", "Gerar", "Cancelar")) return;
                Build();
            }
            EditorSceneManager.OpenScene(PersistenciaPath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Debug.Log("[Somnium] Escritório Azul aberto junto com a persistencia. Dê Play: você entra pela porta sul.");
        }

        [MenuItem("Somnium/Salas/Gerar blockout do Escritório Azul (sobrescreve)")]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Escritório Azul", "A cena já existe. Gerar de novo APAGA as edições feitas à mão nela. Continuar?", "Gerar de novo", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Open();
        }

        [MenuItem("Somnium/Salas/Renderizar prévias do Escritório Azul")]
        public static void RenderPreviewsFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorUtility.RevealInFinder(RenderPreviews());
        }

        /// <summary>Batch: regera o Saguão (porta ligada) e o Escritório, com as prévias do Escritório.</summary>
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
            SetupEnvironment();

            root = new GameObject("EscritorioAzul").transform;
            root.position = Origin;
            root.gameObject.AddComponent<RoomSettings>().roomName = "Escritório Azul";

            BuildShell();
            BuildCabinets();
            BuildDesks();
            BuildTrapdoor();
            BuildCloset();
            BuildLights();

            var spawn = RoomKit.Child(root, "PlayerSpawn", new Vector3(0f, 0f, -HalfD + 1.2f));
            spawn.gameObject.AddComponent<PlayerSpawnPoint>().id = "inicio";

            EditorSceneManager.SaveScene(scene, ScenePath);
            RoomKit.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Somnium] Blockout do Escritório Azul salvo em {ScenePath}.");
        }

        static void SetupEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.25f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.fogColor = new Color(0.15f, 0.2f, 0.3f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        }

        // ------------------------------------------------------------------ casca

        static void BuildShell()
        {
            var wall = kit.Mat("Escritorio_Parede", new Color(0.36f, 0.46f, 0.64f), 0.15f);
            var band = kit.Mat("Escritorio_Rodameio", new Color(0.2f, 0.27f, 0.42f), 0.3f);
            var ceil = kit.Mat("Escritorio_Forro", new Color(0.55f, 0.63f, 0.76f), 0.1f);
            var floor = kit.Checker("Escritorio_Piso", new Color(0.2f, 0.27f, 0.42f), new Color(0.27f, 0.36f, 0.54f), 0.35f);
            var shell = RoomKit.Child(root, "Casca", Vector3.zero);

            // Piso em placas (limite de 4 luzes por objeto no celular), com o buraco do alçapão.
            var fl = RoomKit.Child(shell, "Piso", Vector3.zero);
            float[] xs = { -HalfW, -2f, 2f, HalfW };
            float[] zs = { -HalfD, -6.6f, -2.2f, 2.2f, 6.6f, HalfD };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 5; j++)
                {
                    if (i == 1 && j == 4) continue; // placa do alçapão: feita em volta do buraco
                    RoomKit.FloorQuad(fl, $"Piso_{i}{j}", xs[i], xs[i + 1], zs[j], zs[j + 1], 0f, 1f, floor, groundLayer);
                }
            RoomKit.FloorQuad(fl, "Piso_AlcapaoEsq", -2f, -HatchHalf, 6.6f, HalfD, 0f, 1f, floor, groundLayer);
            RoomKit.FloorQuad(fl, "Piso_AlcapaoDir", HatchHalf, 2f, 6.6f, HalfD, 0f, 1f, floor, groundLayer);
            RoomKit.FloorQuad(fl, "Piso_AlcapaoFrente", -HatchHalf, HatchHalf, 6.6f, HatchZ0, 0f, 1f, floor, groundLayer);
            RoomKit.FloorQuad(fl, "Piso_AlcapaoFundo", -HatchHalf, HatchHalf, HatchZ1, HalfD, 0f, 1f, floor, groundLayer);

            RoomKit.Box(shell, "Forro", new Vector3(0f, Height + 0.1f, 0f), new Vector3(HalfW * 2f + 2 * T, 0.2f, HalfD * 2f + 2 * T), ceil);

            // Paredes (sul com a porta de entrada) + faixa de rodameio.
            void WallX(string n, float z, float x0, float x1, float y0, float y1, Material m) =>
                RoomKit.Box(shell, n, new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, z), new Vector3(x1 - x0, y1 - y0, T), m);
            void WallZ(string n, float x, float z0, float z1) =>
                RoomKit.Box(shell, n, new Vector3(x, Height / 2f, (z0 + z1) / 2f), new Vector3(T, Height, z1 - z0), wall);

            float zs0 = -HalfD - T / 2f, zn = HalfD + T / 2f;
            WallX("Parede_Norte", zn, -HalfW - T, HalfW + T, 0f, Height, wall);
            WallX("Parede_Sul_Esq", zs0, -HalfW - T, -DoorHalfW, 0f, Height, wall);
            WallX("Parede_Sul_Dir", zs0, DoorHalfW, HalfW + T, 0f, Height, wall);
            WallX("Parede_Sul_Topo", zs0, -DoorHalfW, DoorHalfW, DoorH, Height, wall);
            WallZ("Parede_Oeste", -HalfW - T / 2f, -HalfD, HalfD);
            WallZ("Parede_Leste", HalfW + T / 2f, -HalfD, HalfD);

            var bands = RoomKit.Child(shell, "Rodameio", Vector3.zero);
            RoomKit.Box(bands, "Norte", new Vector3(0f, 0.5f, HalfD - 0.01f), new Vector3(HalfW * 2f, 1f, 0.02f), band, false);
            RoomKit.Box(bands, "Oeste", new Vector3(-HalfW + 0.01f, 0.5f, 0f), new Vector3(0.02f, 1f, HalfD * 2f), band, false);
            RoomKit.Box(bands, "Leste", new Vector3(HalfW - 0.01f, 0.5f, 0f), new Vector3(0.02f, 1f, HalfD * 2f), band, false);
            RoomKit.Box(bands, "Sul_Esq", new Vector3(-(HalfW + DoorHalfW) / 2f, 0.5f, -HalfD + 0.01f), new Vector3(HalfW - DoorHalfW, 1f, 0.02f), band, false);
            RoomKit.Box(bands, "Sul_Dir", new Vector3((HalfW + DoorHalfW) / 2f, 0.5f, -HalfD + 0.01f), new Vector3(HalfW - DoorHalfW, 1f, 0.02f), band, false);

            // Porta de entrada (volta ao Saguão).
            var doorMat = kit.Mat("Escritorio_Porta", new Color(0.22f, 0.3f, 0.48f), 0.3f);
            var glass = kit.Mat("Escritorio_Vidro", new Color(0.62f, 0.72f, 0.86f), 0.8f);
            var d = RoomKit.Child(shell, "Porta_Saguao", new Vector3(0f, 0f, -HalfD - 0.05f));
            var leaf = RoomKit.Box(d, "Folha", new Vector3(0f, DoorH / 2f, 0f), new Vector3(DoorHalfW * 2f, DoorH, 0.06f), doorMat);
            RoomKit.Box(d, "Vidro", new Vector3(0f, 1.6f, 0.035f), new Vector3(0.8f, 0.6f, 0.01f), glass, false);
            RoomKit.SignText(d, "ARQUIVO", new Vector3(0f, 1.6f, 0.045f), new Vector2(700f, 160f), 110, new Color(0.12f, 0.17f, 0.3f), 180f);
            var back = leaf.AddComponent<RoomDoor>();
            back.targetScenePath = SaguaoBuilder.ScenePath;
            back.spawnId = SaguaoBuilder.SpawnFromEscritorio;
            RoomKit.SetPrompt(back, "Sair (voltar ao Saguão)");
        }

        // ------------------------------------------------------------------ arquivos de metal

        /// <summary>Malha de um trecho de 6 arquivos de 4 gavetas (frente para +Z), salva como asset e reaproveitada.</summary>
        static Mesh CabinetSegmentMesh()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(cube);

            var parts = new List<CombineInstance>();
            void Part(Vector3 pos, Vector3 size) => parts.Add(new CombineInstance { mesh = cubeMesh, transform = Matrix4x4.TRS(pos, Quaternion.identity, size) });

            for (int k = 0; k < 6; k++)
            {
                float x = -1.25f + k * 0.5f;
                Part(new Vector3(x, 0.66f, 0f), new Vector3(0.48f, 1.32f, 0.62f));
                for (int g = 0; g < 4; g++)
                {
                    float y = 0.21f + g * 0.31f;
                    Part(new Vector3(x, y, 0.32f), new Vector3(0.42f, 0.26f, 0.025f));
                    Part(new Vector3(x, y + 0.06f, 0.345f), new Vector3(0.12f, 0.025f, 0.03f));
                    Part(new Vector3(x, y - 0.04f, 0.335f), new Vector3(0.08f, 0.035f, 0.005f)); // etiqueta
                }
            }
            var mesh = new Mesh { name = "Arquivos6", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return RoomKit.SaveMesh(mesh, $"{GenFolder}/Arquivos6.asset");
        }

        static void BuildCabinets()
        {
            var metal = kit.Mat("Escritorio_ArquivoMetal", new Color(0.3f, 0.4f, 0.6f), 0.45f, 0.35f);
            var mesh = CabinetSegmentMesh();
            var g = RoomKit.Child(root, "Arquivos", Vector3.zero);

            // Três fileiras de cada lado, todas viradas para o corredor central; um corredor transversal em z = 1..2,2.
            float[] rowsX = { 1.31f, 3.33f, 5.69f };
            float[] segZ = { -3.5f, -0.5f, 3.7f, 6.7f };
            foreach (float side in new[] { -1f, 1f })
                foreach (float rx in rowsX)
                    foreach (float z in segZ)
                    {
                        var seg = new GameObject($"Fileira_{(side < 0 ? "O" : "L")}{rx:0.0}_{z:0.0}");
                        seg.transform.SetParent(g, false);
                        seg.transform.localPosition = new Vector3(side * rx, 0f, z);
                        seg.transform.localRotation = Quaternion.Euler(0f, side < 0 ? 90f : -90f, 0f);
                        seg.AddComponent<MeshFilter>().sharedMesh = mesh;
                        seg.AddComponent<MeshRenderer>().sharedMaterial = metal;
                        var c = seg.AddComponent<BoxCollider>();
                        c.center = new Vector3(0f, 0.66f, 0f);
                        c.size = new Vector3(3f, 1.32f, 0.66f);
                        GameObjectUtility.SetStaticEditorFlags(seg, StaticEditorFlags.BatchingStatic);
                    }
        }

        // ------------------------------------------------------------------ mesas

        static void BuildDesks()
        {
            var deskMat = kit.Mat("Escritorio_Mesa", new Color(0.25f, 0.33f, 0.52f), 0.35f);
            var chairMat = kit.Mat("Escritorio_Cadeira", new Color(0.18f, 0.24f, 0.38f), 0.3f);
            var typeMat = kit.Mat("Escritorio_Maquina", new Color(0.12f, 0.15f, 0.24f), 0.5f, 0.3f);
            var paper = kit.Mat("Escritorio_Papel", new Color(0.86f, 0.88f, 0.92f), 0.1f);
            var g = RoomKit.Child(root, "Mesas", Vector3.zero);

            var spots = new[] { new Vector3(-3.4f, 0f, -9.3f), new Vector3(3.4f, 0f, -9.3f), new Vector3(-3.4f, 0f, -6.9f), new Vector3(3.4f, 0f, -6.9f) };
            for (int i = 0; i < spots.Length; i++)
            {
                var p = spots[i];
                var desk = RoomKit.Model($"{ModelFolder}/escrivaninha.fbx", g, $"Mesa_{i + 1}", p, 0f, 0.76f, true, deskMat);
                float top = 0.76f;
                if (desk == null)
                    desk = RoomKit.Box(g, $"Mesa_{i + 1}", p + new Vector3(0f, 0.38f, 0f), new Vector3(1.8f, 0.76f, 0.75f), deskMat);
                else
                {
                    top = RoomKit.WorldBounds(desk).max.y - Origin.y;
                    var b = RoomKit.WorldBounds(desk);
                    var col = desk.AddComponent<BoxCollider>();
                    col.center = desk.transform.InverseTransformPoint(b.center);
                    col.size = b.size;
                }

                // Máquina de escrever + folha.
                RoomKit.Box(g, $"Maquina_{i + 1}", p + new Vector3(0.25f, top + 0.07f, 0f), new Vector3(0.42f, 0.14f, 0.34f), typeMat, false);
                RoomKit.Box(g, $"Folha_{i + 1}", p + new Vector3(0.25f, top + 0.2f, 0.12f), new Vector3(0.22f, 0.22f, 0.005f), paper, false)
                    .transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

                var chair = RoomKit.Model($"{ModelFolder}/cadeira.fbx", g, $"Cadeira_{i + 1}", p + new Vector3(0f, 0f, -0.75f), 0f, 0.95f, true, chairMat);
                if (chair == null) RoomKit.Box(g, $"Cadeira_{i + 1}", p + new Vector3(0f, 0.45f, -0.75f), new Vector3(0.45f, 0.9f, 0.45f), chairMat);
            }

            // Memorando na primeira mesa: dá a pista do alçapão.
            var memo = RoomKit.Box(g, "Memorando", spots[0] + new Vector3(-0.45f, 0.765f, 0.05f), new Vector3(0.21f, 0.004f, 0.3f), paper);
            memo.GetComponent<BoxCollider>().size = new Vector3(1.6f, 40f, 1.4f);
            var msg = memo.AddComponent<MessageInteractable>();
            msg.message = "MEMORANDO, 1954: \"Por ordem da diretoria, o alçapão do arquivo morto (fim do corredor central) foi acorrentado. Ninguém desce lá.\"";
            msg.duration = 6f;
            RoomKit.SetPrompt(msg, "Ler memorando");
        }

        // ------------------------------------------------------------------ alçapão (Alicate → Chave 2 + Lata de Ração)

        static void BuildTrapdoor()
        {
            var wood = kit.Mat("Escritorio_Alcapao", new Color(0.24f, 0.3f, 0.45f), 0.3f);
            var pitMat = kit.Mat("Escritorio_Poço", new Color(0.1f, 0.13f, 0.2f), 0.2f);
            var chainMat = kit.Mat("Escritorio_Corrente", new Color(0.62f, 0.64f, 0.68f), 0.6f, 0.9f);
            var brass = kit.Mat("Escritorio_Latao", new Color(0.85f, 0.68f, 0.25f), 0.6f, 0.9f);
            var gold = kit.Mat("Escritorio_Dourado", new Color(0.9f, 0.72f, 0.28f), 0.7f, 1f);
            var tin = kit.Mat("Escritorio_Lata", new Color(0.82f, 0.82f, 0.85f), 0.6f, 0.8f);
            var labelMat = kit.Mat("Escritorio_RotuloLata", new Color(0.95f, 0.75f, 0.15f), 0.3f);

            float zc = (HatchZ0 + HatchZ1) / 2f;
            var g = RoomKit.Child(root, "Alcapao", Vector3.zero);

            // Nicho embaixo do piso.
            var pit = RoomKit.Child(g, "Nicho", Vector3.zero);
            RoomKit.Box(pit, "Fundo", new Vector3(0f, -PitDepth - 0.05f, zc), new Vector3(1f, 0.1f, 1f), pitMat).layer = groundLayer;
            // Paredes do nicho terminam 1 cm abaixo do piso: topo na mesma altura do piso dava z-fighting (borda serrilhada).
            const float WallH = PitDepth - 0.01f, WallY = -0.01f - WallH / 2f;
            RoomKit.Box(pit, "Lado_Esq", new Vector3(-HatchHalf - 0.025f, WallY, zc), new Vector3(0.05f, WallH, 1.1f), pitMat);
            RoomKit.Box(pit, "Lado_Dir", new Vector3(HatchHalf + 0.025f, WallY, zc), new Vector3(0.05f, WallH, 1.1f), pitMat);
            RoomKit.Box(pit, "Lado_Frente", new Vector3(0f, WallY, HatchZ0 - 0.025f), new Vector3(1f, WallH, 0.05f), pitMat);
            RoomKit.Box(pit, "Lado_Fundo", new Vector3(0f, WallY, HatchZ1 + 0.025f), new Vector3(1f, WallH, 0.05f), pitMat);
            // Grade invisível sobre o buraco: o jogador não cai lá dentro (layer Ignore Raycast: a mira passa).
            var guard = RoomKit.Volume(pit, "Grade_Invisivel", new Vector3(0f, -0.05f, zc), new Vector3(1f, 0.1f, 1f), false);
            guard.layer = 2;

            // Itens no fundo do nicho.
            // Chave 2: objeto sem escala com o item e a colisão; bloco e brilho como filhos (somem juntos ao pegar).
            var key = RoomKit.Child(pit, "Pickup_Chave2", new Vector3(-0.2f, -PitDepth + 0.02f, zc - 0.1f));
            key.localRotation = Quaternion.Euler(0f, 25f, 0f);
            RoomKit.Box(key, "Chave", Vector3.zero, new Vector3(0.18f, 0.03f, 0.06f), gold, false);
            var kc = key.gameObject.AddComponent<BoxCollider>(); kc.size = new Vector3(0.36f, 0.24f, 0.3f); kc.isTrigger = true;
            var kp = key.gameObject.AddComponent<ItemPickup>(); kp.item = RoomKit.Item("Chave2"); kp.saveId = "escritorio/chave2";
            // O nicho é escuro: um brilho que pisca de vez em quando, como na Manivela da Colinas.
            var glint = RoomKit.Prim(PrimitiveType.Quad, key, "Brilho", new Vector3(0.03f, 0.06f, 0f), Vector3.one * 0.1f,
                                     kit.Emissive("Escritorio_Brilho", new Color(1f, 0.97f, 0.85f), 3f));
            glint.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glint.AddComponent<Brilho>().maxScale = 0.1f;

            var can = RoomKit.Prim(PrimitiveType.Cylinder, pit, "Pickup_LataRacao", new Vector3(0.22f, -PitDepth + 0.06f, zc + 0.15f), new Vector3(0.16f, 0.06f, 0.16f), tin);
            RoomKit.Prim(PrimitiveType.Cylinder, can.transform, "Rotulo", Vector3.zero, new Vector3(1.02f, 0.6f, 1.02f), labelMat);
            var cc = can.AddComponent<BoxCollider>(); cc.size = new Vector3(2.2f, 3f, 2.2f); cc.isTrigger = true;
            var cp = can.AddComponent<ItemPickup>(); cp.item = RoomKit.Item("LataDeRacao"); cp.saveId = "escritorio/lata";

            // Tampa com dobradiça na borda do fundo; abre para cima.
            var hinge = RoomKit.Child(g, "Tampa_Dobradica", new Vector3(0f, 0f, HatchZ1));
            var lid = RoomKit.Box(hinge, "Tampa", new Vector3(0f, -0.03f, -0.5f), new Vector3(1f, 0.06f, 1f), wood);
            lid.layer = groundLayer;
            RoomKit.Box(hinge, "Puxador", new Vector3(0f, 0.02f, -0.85f), new Vector3(0.18f, 0.03f, 0.04f), brass, false);
            var rot = hinge.gameObject.AddComponent<GirarAoAtivar>();
            rot.openLocalEuler = new Vector3(110f, 0f, 0f);

            // Corrente em X com cadeado.
            var chain = RoomKit.Child(g, "Corrente", new Vector3(0f, 0.035f, zc));
            foreach (float a in new[] { 45f, -45f })
                RoomKit.Box(chain, $"Elo_{a}", Vector3.zero, new Vector3(1.35f, 0.03f, 0.05f), chainMat, false).transform.localRotation = Quaternion.Euler(0f, a, 0f);
            RoomKit.Box(chain, "Cadeado", new Vector3(0f, 0.06f, 0f), new Vector3(0.1f, 0.12f, 0.05f), brass, false);
            foreach (var c in new[] { new Vector3(-0.62f, 0f, -0.62f), new Vector3(0.62f, 0f, -0.62f), new Vector3(-0.62f, 0f, 0.62f), new Vector3(0.62f, 0f, 0.62f) })
                RoomKit.Box(chain, "Argola", c, new Vector3(0.07f, 0.05f, 0.07f), chainMat, false);

            var area = RoomKit.Volume(g, "Trava_Alcapao", new Vector3(0f, 0.3f, zc), new Vector3(1.4f, 0.6f, 1.4f), true);
            var lockComp = area.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { RoomKit.Item("Alicate") };
            lockComp.consumeItems = true;
            lockComp.usablePromptText = "Cortar a corrente";
            lockComp.lockedMessage = "Um alçapão no chão, preso por uma corrente grossa com cadeado. Daria para cortar com alguma ferramenta.";
            lockComp.unlockedMessage = "O alicate corta a corrente. A tampa se abre: há algo lá embaixo.";
            lockComp.deactivateOnUnlock = new[] { chain.gameObject };
            lockComp.saveId = "escritorio/alcapao";
            RoomKit.SetPrompt(lockComp, "Examinar o alçapão");
            UnityEventTools.AddPersistentListener(lockComp.onUnlocked, rot.Play);
            UnityEventTools.AddPersistentListener(lockComp.onRestoredUnlocked, rot.SnapOpen);
        }

        // ------------------------------------------------------------------ almoxarifado com o galão de ácido

        static void BuildCloset()
        {
            var body = kit.Mat("Escritorio_Armario", new Color(0.26f, 0.34f, 0.52f), 0.4f, 0.2f);
            var inside = kit.Mat("Escritorio_ArmarioDentro", new Color(0.12f, 0.15f, 0.22f), 0.2f);
            var jug = kit.Mat("Escritorio_Galao", new Color(0.92f, 0.92f, 0.88f), 0.3f);
            var g = RoomKit.Child(root, "Almoxarifado", ClosetPos);

            const float w = 1.2f, h = 2.1f, d = 0.6f;
            RoomKit.Box(g, "Fundo", new Vector3(0f, h / 2f, d / 2f - 0.03f), new Vector3(w, h, 0.06f), inside);
            RoomKit.Box(g, "Lado_Esq", new Vector3(-w / 2f + 0.02f, h / 2f, 0f), new Vector3(0.04f, h, d), body);
            RoomKit.Box(g, "Lado_Dir", new Vector3(w / 2f - 0.02f, h / 2f, 0f), new Vector3(0.04f, h, d), body);
            RoomKit.Box(g, "Topo", new Vector3(0f, h - 0.02f, 0f), new Vector3(w, 0.04f, d), body);
            RoomKit.Box(g, "Base", new Vector3(0f, 0.03f, 0f), new Vector3(w, 0.06f, d), inside);
            RoomKit.Box(g, "Prateleira", new Vector3(0f, 1.3f, 0f), new Vector3(w - 0.08f, 0.03f, d - 0.08f), inside);
            RoomKit.SignText(g, "ALMOXARIFADO", new Vector3(0f, h + 0.12f, -d / 2f - 0.01f), new Vector2(1100f, 180f), 120, new Color(0.85f, 0.9f, 1f), 0f);

            // Portas de duas folhas (Itr_Door do Caio): abrem para fora, sem chave.
            foreach (var (name, hingeX, dir, angle) in new[] { ("Porta_Esq", -w / 2f, 1f, 100f), ("Porta_Dir", w / 2f, -1f, -100f) })
            {
                var pivot = RoomKit.Child(g, name, new Vector3(hingeX, 0.06f, -d / 2f - 0.02f));
                RoomKit.Box(pivot, "Folha", new Vector3(dir * w / 4f, (h - 0.1f) / 2f, 0f), new Vector3(w / 2f - 0.01f, h - 0.1f, 0.03f), body);
                RoomKit.Box(pivot, "Puxador", new Vector3(dir * (w / 2f - 0.08f), 1.05f, -0.03f), new Vector3(0.025f, 0.14f, 0.025f), kit.Mat("Escritorio_Latao", new Color(0.85f, 0.68f, 0.25f), 0.6f, 0.9f), false);
                var door = pivot.gameObject.AddComponent<Itr_Door>();
                door.openAngle = angle;
                door.openSpeed = 4f;
                RoomKit.SetPrompt(door, "Abrir/fechar o armário");
            }

            // Galão de ácido (modelo do grupo) com rótulo de perigo amarelo e preto.
            var jugGo = RoomKit.Model($"{ModelFolder}/galao.fbx", g, "Galao_Acido", new Vector3(0f, 0.06f, 0.02f), 90f, 0.5f, true, jug);
            if (jugGo == null) jugGo = RoomKit.Box(g, "Galao_Acido", new Vector3(0f, 0.31f, 0.02f), new Vector3(0.3f, 0.5f, 0.22f), jug, false);
            var jb = RoomKit.WorldBounds(jugGo);
            var label = RoomKit.Box(g, "Rotulo_Perigo", new Vector3(0f, 0.06f + jb.size.y * 0.45f, 0.02f - jb.size.z / 2f - 0.006f),
                                    new Vector3(Mathf.Min(0.26f, jb.size.x * 0.8f), 0.16f, 0.006f), kit.Mat("Escritorio_RotuloPerigo", new Color(0.98f, 0.82f, 0.1f), 0.3f), false);
            RoomKit.SignText(label.transform.parent, "PERIGO\nÁCIDO", label.transform.localPosition + new Vector3(0f, 0f, -0.005f), new Vector2(260f, 160f), 52, Color.black, 0f);

            var col = jugGo.AddComponent<BoxCollider>();
            col.center = jugGo.transform.InverseTransformPoint(jb.center);
            col.size = jb.size + new Vector3(0.1f, 0.1f, 0.1f);
            var lockComp = jugGo.AddComponent<ItemLock>();
            lockComp.requiredItems = new List<ItemData> { RoomKit.Item("CartuchoVazio") };
            lockComp.consumeItems = true;
            lockComp.usablePromptText = "Encher o cartucho com ácido";
            lockComp.lockedMessage = "Um galão com rótulo de perigo: ÁCIDO. Precisaria de algo para guardar um pouco.";
            lockComp.unlockedMessage = "Você enche o cartucho com o ácido, com cuidado.";
            lockComp.alreadyUnlockedMessage = "O galão ainda tem bastante ácido.";
            lockComp.rewardItem = RoomKit.Item("Herbicida");
            lockComp.disableAfterUnlock = false;
            lockComp.saveId = "escritorio/galao";
            RoomKit.SetPrompt(lockComp, "Examinar o galão");
        }

        // ------------------------------------------------------------------ luz fluorescente

        static void BuildLights()
        {
            var on = kit.Emissive("Escritorio_LuzAcesa", new Color(0.85f, 0.92f, 1f), 2.2f);
            var off = kit.Mat("Escritorio_LuzApagada", new Color(0.3f, 0.33f, 0.4f), 0.4f);
            var g = RoomKit.Child(root, "Iluminacao", Vector3.zero);

            var spots = new List<Vector3>
            {
                new Vector3(0f, 0f, -8.5f), new Vector3(0f, 0f, -4f), new Vector3(0f, 0f, 0.5f), new Vector3(0f, 0f, 5f), new Vector3(0f, 0f, 9.4f),
                new Vector3(-4.5f, 0f, -1.5f), new Vector3(4.5f, 0f, -1.5f), new Vector3(-4.5f, 0f, 5f), new Vector3(4.5f, 0f, 5f),
                new Vector3(-3.4f, 0f, -8.1f), new Vector3(3.4f, 0f, -8.1f),
            };
            for (int i = 0; i < spots.Count; i++)
            {
                var s = RoomKit.Child(g, $"Luminaria_{i:00}", spots[i]);
                var panel = RoomKit.Box(s, "Painel", new Vector3(0f, Height - 0.02f, 0f), new Vector3(0.3f, 0.03f, 1.2f), on, false);
                panel.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                var light = RoomKit.PointLight(s, "Luz", new Vector3(0f, Height - 0.3f, 0f), LightColor, 2.2f, 6.5f);
                if (i == 3) // a do meio do corredor central pisca
                {
                    var f = s.gameObject.AddComponent<LuzPiscando>();
                    f.target = light;
                    f.panel = panel.GetComponent<MeshRenderer>();
                    f.onMaterial = on;
                    f.offMaterial = off;
                }
            }
        }

        // ------------------------------------------------------------------ prévias

        public static string RenderPreviews()
        {
            string dir = RoomKit.PreviewDir();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Vector3 O(float x, float y, float z) => Origin + new Vector3(x, y, z);
            float zc = (HatchZ0 + HatchZ1) / 2f;

            using (var s = new RoomKit.Shooter(dir, "escritorio", O(0f, 1.65f, -9.8f), false))
            {
                s.Shot("1_entrada", O(0f, 1.65f, -9.8f), O(0f, 1.2f, 10f));
                s.Shot("2_mesas", O(0f, 1.65f, -10.3f), O(-3.4f, 0.8f, -8f));
                s.Shot("3_corredor_lateral", O(-4.5f, 1.65f, -4.5f), O(-4.5f, 1.2f, 10f));
                s.Shot("4_alcapao", O(0f, 1.65f, zc - 2.2f), O(0f, 0f, zc));
                s.Shot("5_almoxarifado", O(2.6f, 1.65f, 8.6f), ClosetPos + Origin + new Vector3(0f, 1f, 0f));

                // Estado resolvido: corrente cortada, tampa aberta, armário aberto.
                var root = GameObject.Find("EscritorioAzul").transform;
                root.Find("Alcapao/Corrente").gameObject.SetActive(false);
                root.Find("Alcapao/Tampa_Dobradica").localRotation = Quaternion.Euler(110f, 0f, 0f);
                root.Find("Almoxarifado/Porta_Esq").localRotation = Quaternion.Euler(0f, 100f, 0f);
                root.Find("Almoxarifado/Porta_Dir").localRotation = Quaternion.Euler(0f, -100f, 0f);
                s.Shot("6_alcapao_aberto", O(0.3f, 1.65f, zc - 1.6f), O(0f, -0.6f, zc));
                s.Shot("7_armario_aberto", O(3.4f, 1.65f, 8.9f), ClosetPos + Origin + new Vector3(0f, 0.4f, 0f));
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[Somnium] Prévias do Escritório Azul salvas em {System.IO.Path.GetFullPath(dir)}");
            return System.IO.Path.GetFullPath(dir);
        }
    }
}
