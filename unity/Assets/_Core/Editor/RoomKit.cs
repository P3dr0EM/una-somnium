using System.Collections.Generic;
using System.IO;
using System.Linq;
using Somnium.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Somnium.EditorTools.Rooms
{
    /// <summary>
    /// Ferramentas comuns dos geradores de sala (Rooms/*/Editor/*Builder.cs): blocos, materiais URP,
    /// modelos importados com escala automática, textos de placa, prévias em PNG e lista de cenas.
    /// Uso: var kit = new RoomKit("Assets/Rooms/02_EscritorioAzul/Materials");
    /// </summary>
    public class RoomKit
    {
        public const string ItemsFolder = "Assets/Data/Items";

        readonly string matFolder;
        readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        public RoomKit(string materialFolder)
        {
            matFolder = materialFolder;
            EnsureFolder(matFolder);
        }

        // ---------------- hierarquia e blocos

        public static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static GameObject Box(Transform parent, string name, Vector3 localPos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Place(go, parent, name, localPos, size, mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            Place(go, parent, name, localPos, scale, mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static void Place(GameObject go, Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        /// <summary>Caixa invisível só de colisão (ou gatilho).</summary>
        public static GameObject Volume(Transform parent, string name, Vector3 localPos, Vector3 size, bool trigger)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var c = go.AddComponent<BoxCollider>();
            c.size = size;
            c.isTrigger = trigger;
            return go;
        }

        public static Light PointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range, LightShadows shadows = LightShadows.None)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows;
            return l;
        }

        // ---------------- modelos importados

        /// <summary>
        /// Instancia um modelo (FBX/OBJ) e ajusta a escala para que a maior medida (ou a altura, se <paramref name="byHeight"/>)
        /// tenha <paramref name="targetSize"/> m. A base fica no chão e o centro no pivô. Tira colliders do modelo.
        /// Retorna null se o arquivo não existir (o gerador usa blocos no lugar).
        /// </summary>
        public static GameObject Model(string path, Transform parent, string name, Vector3 localPos, float yaw, float targetSize, bool byHeight, Material mat)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[Somnium] Modelo não encontrado: {path}");
                return null;
            }
            var holder = Child(parent, name, localPos);
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            m.name = "Modelo";
            m.transform.SetParent(holder, false);
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (mat != null) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();

            var b = WorldBounds(m);
            float size = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (size > 1e-4f) m.transform.localScale *= targetSize / size;
            b = WorldBounds(m);
            m.transform.position += new Vector3(holder.position.x - b.center.x, holder.position.y - b.min.y, holder.position.z - b.center.z);
            return holder.gameObject;
        }

        /// <summary>
        /// Encaixa um modelo exatamente numa caixa (centro e tamanho no espaço local do <paramref name="parent"/>, que não pode
        /// estar girado no momento): escala diferente por eixo. <paramref name="yaw"/> gira o modelo antes (múltiplos de 90°).
        /// Hierarquia: holder (posição) → fit (escala por eixo) → rot (giro) → modelo. Retorna o holder, ou null sem o arquivo.
        /// </summary>
        public static GameObject FitModel(string path, Transform parent, string name, Vector3 center, Vector3 size, float yaw, Material mat)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) { Debug.LogWarning($"[Somnium] Modelo não encontrado: {path}"); return null; }
            var holder = Child(parent, name, center);
            var fit = Child(holder, "Escala", Vector3.zero);
            var rot = Child(fit, "Giro", Vector3.zero);
            rot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            m.name = "Modelo";
            m.transform.SetParent(rot, false);
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (mat != null) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();

            // Medido no espaço do holder: funciona mesmo com o pai girado (ex.: os braços da corrente em X).
            var b = LocalBounds(m, holder);
            Debug.Log($"[Somnium] FitModel {name}: tamanho original {b.size} → alvo {size}");
            fit.localScale = new Vector3(size.x / Mathf.Max(1e-4f, b.size.x), size.y / Mathf.Max(1e-4f, b.size.y), size.z / Mathf.Max(1e-4f, b.size.z));
            b = LocalBounds(m, holder);
            m.transform.position += holder.TransformVector(-b.center);
            return holder.gameObject;
        }

        public const string KeyModelPath = "Assets/Shared/Models/Itens/chave.fbx";

        /// <summary>
        /// Chave do grupo (chave.fbx), deitada: comprimento ao longo do X local e centro no pivô, escala uniforme.
        /// <paramref name="orient"/> gira a chave inteira depois (ex.: em pé, enfiada na fechadura). Null sem o arquivo.
        /// </summary>
        public static GameObject Key(Transform parent, string name, Vector3 localPos, float length, Material mat, Quaternion orient)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(KeyModelPath);
            if (asset == null) { Debug.LogWarning($"[Somnium] Modelo não encontrado: {KeyModelPath}"); return null; }
            var holder = Child(parent, name, localPos);
            holder.localRotation = orient;
            holder.gameObject.AddComponent<Somnium.Core.NaoAssar>(); // pequena e aparece/some: luz pelas probes, fora do bake
            var rot = Child(holder, "Giro", Vector3.zero);
            var m = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            m.name = "Modelo";
            m.transform.SetParent(rot, false);
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            if (mat != null) foreach (var r in m.GetComponentsInChildren<Renderer>()) r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
            // Comprimento no X e espessura no Y (deitada).
            var b = LocalBounds(m, holder);
            if (b.size.z > b.size.x) rot.localRotation = Quaternion.Euler(0f, 90f, 0f);
            b = LocalBounds(m, holder);
            if (b.size.y > b.size.z) rot.localRotation = Quaternion.Euler(90f, 0f, 0f) * rot.localRotation;
            b = LocalBounds(m, holder);
            rot.localScale = Vector3.one * (length / Mathf.Max(1e-4f, b.size.x));
            b = LocalBounds(m, holder);
            m.transform.position += holder.TransformVector(-b.center);
            return holder.gameObject;
        }

        public static GameObject Key(Transform parent, string name, Vector3 localPos, float length, Material mat) =>
            Key(parent, name, localPos, length, mat, Quaternion.identity);

        /// <summary>Caixa dos vértices de um modelo no espaço local de <paramref name="space"/>.</summary>
        public static Bounds LocalBounds(GameObject go, Transform space)
        {
            bool any = false;
            var b = new Bounds();
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var p = space.InverseTransformPoint(mf.transform.TransformPoint(v));
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // ---------------- materiais

        static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        public Material Mat(string name, Color color, float smoothness, float metallic = 0f)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{matFolder}/{name}.mat";
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

        public Material Emissive(string name, Color color, float intensity)
        {
            var mat = Mat(name, color, 0f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Material com textura xadrez gerada (período em metros = 2 casas). Use com UVs em metros (FloorQuad).</summary>
        public Material Checker(string name, Color a, Color b, float smoothness)
        {
            string texPath = $"{matFolder}/{name}.png";
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, ((x / (size / 2)) + (y / (size / 2))) % 2 == 0 ? a : b);
            tex.Apply();
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var mat = Mat(name, Color.white, smoothness);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>
        /// Placa de piso (só a face de cima) com UV em metros: a textura casa entre placas vizinhas.
        /// Ganha um BoxCollider fino e a layer pedida. <paramref name="period"/> = metros por repetição da textura.
        /// </summary>
        public static GameObject FloorQuad(Transform parent, string name, float x0, float x1, float z0, float z1, float y, float period, Material mat, int layer)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0) };
            mesh.uv = new[] { new Vector2(x0, z0) / period, new Vector2(x0, z1) / period, new Vector2(x1, z1) / period, new Vector2(x1, z0) / period };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.normals = Enumerable.Repeat(Vector3.up, 4).ToArray();
            mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = layer;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var c = go.AddComponent<BoxCollider>();
            c.center = new Vector3((x0 + x1) / 2f, y - 0.1f, (z0 + z1) / 2f);
            c.size = new Vector3(x1 - x0, 0.2f, z1 - z0);
            return go;
        }

        // ---------------- interação e itens

        public static void SetPrompt(Assets.Scripts.Interactable target, string text)
        {
            var so = new SerializedObject(target);
            so.FindProperty("promptText").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static ItemData Item(string id)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>($"{ItemsFolder}/{id}.asset");
            if (item == null) Debug.LogWarning($"[Somnium] Item '{id}' não encontrado. Rode Somnium > Itens > Criar itens do roteiro.");
            return item;
        }

        /// <summary>Texto em canvas de mundo (placas, rótulos). 1 px = 1 mm.</summary>
        public static Text SignText(Transform parent, string text, Vector3 localPos, Vector2 sizeMm, int fontSize, Color color, float yaw = 0f)
        {
            var go = new GameObject("Texto_" + text.Split('\n')[0], typeof(RectTransform), typeof(Canvas));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localPosition = localPos;
            rt.localRotation = Quaternion.Euler(0f, yaw, 0f);
            rt.localScale = Vector3.one * 0.001f;
            rt.sizeDelta = sizeMm;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            var trt = t.rectTransform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.text = text;
            return t;
        }

        // ---------------- prévias

        /// <summary>Pasta de saída das prévias: argumento "-previewOut pasta" ou Temp/Previews.</summary>
        public static string PreviewDir()
        {
            string dir = "Temp/Previews";
            var args = System.Environment.GetCommandLineArgs();
            int idx = System.Array.IndexOf(args, "-previewOut");
            if (idx >= 0 && idx + 1 < args.Length) dir = args[idx + 1];
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Câmera de prévia com aquecimento (os primeiros renders saem com artefatos enquanto os shaders compilam).</summary>
        public class Shooter : System.IDisposable
        {
            readonly Camera cam;
            readonly RenderTexture rt;
            readonly string dir, prefix;
            const int W = 1280, H = 720;

            public Shooter(string dir, string prefix, Vector3 warmPos, bool skybox)
            {
                this.dir = dir;
                this.prefix = prefix;
                cam = new GameObject("PreviewCam").AddComponent<Camera>();
                cam.fieldOfView = 70f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 600f;
                cam.clearFlags = skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.transform.position = warmPos;
                for (int i = 0; i < 3; i++) cam.Render();
            }

            public void Shot(string name, Vector3 pos, Vector3 lookAt)
            {
                cam.transform.position = pos;
                cam.transform.LookAt(lookAt);
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, $"{prefix}_{name}.png"), tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                RenderTexture.active = null;
            }

            public void Dispose()
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(cam.gameObject);
            }
        }

        // ---------------- luz "baked" (assada em textura)

        /// <summary>
        /// Componentes que indicam objeto que se move, aparece/some ou anima: não entram como estáticos no bake
        /// (recebem luz pelas Light Probes).
        /// </summary>
        static readonly System.Type[] DynamicMarkers =
        {
            typeof(Itr_Door), typeof(ItemPickup), typeof(Somnium.Interaction.GirarAoAtivar),
            typeof(Somnium.Interaction.Brilho), typeof(Rigidbody), typeof(Somnium.Core.NaoAssar),
        };

        static bool IsDynamic(Transform t, Transform root)
        {
            for (var p = t; p != null; p = p.parent)
            {
                foreach (var type in DynamicMarkers)
                    if (p.GetComponent(type) != null) return true;
                if (p.GetComponent("GatoTeleporte") != null) return true;
                if (p == root) break;
            }
            return false;
        }

        /// <summary>
        /// Assa a luz da sala: as luzes ativas viram "Baked" (luz direta, sombras e rebatimento gravados em lightmap),
        /// a geometria fixa vira estática e uma grade de Light Probes ilumina o que se move.
        /// Bom para o celular: sombras sem custo e sem o limite de 4 luzes por objeto.
        /// Chame depois de montar a cena e antes de salvar. Exige a cena já salva em <paramref name="scenePath"/>.
        /// </summary>
        public static bool BakeLighting(Transform root, string scenePath, Bounds probeArea, float probeSpacing,
                                        float texelsPerUnit = 10f, int maxLightmapSize = 1024)
        {
            // 1) Estáticos: renderers ativos que não se movem.
            int stat = 0, dyn = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (r.GetComponentInParent<Canvas>() != null) continue;
                bool dynamic = IsDynamic(r.transform, root);
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                if (dynamic)
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags & ~StaticEditorFlags.ContributeGI);
                    dyn++;
                }
                else
                {
                    GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
                    r.receiveGI = ReceiveGI.Lightmaps;
                    stat++;
                }
            }

            // 2) Luzes ativas viram Baked (as desligadas, como a luz do final, continuam em tempo real).
            int baked = 0;
            foreach (var l in root.GetComponentsInChildren<Light>(false))
            {
                if (IsDynamic(l.transform, root)) continue; // ex.: fogo da lareira, que tremula em tempo real
                l.lightmapBakeType = LightmapBakeType.Baked;
                if (l.type == LightType.Point && l.shadows == LightShadows.None) l.shadows = LightShadows.Soft;
                l.shapeRadius = 0.15f;
                baked++;
            }

            // 3) Light Probes em grade (iluminam portas que abrem, itens, o jogador segurando coisas...).
            var probesGo = new GameObject("LightProbes");
            probesGo.transform.SetParent(root, false);
            var group = probesGo.AddComponent<LightProbeGroup>();
            var pts = new List<Vector3>();
            for (float x = probeArea.min.x; x <= probeArea.max.x + 0.01f; x += probeSpacing)
                for (float y = probeArea.min.y; y <= probeArea.max.y + 0.01f; y += probeSpacing)
                    for (float z = probeArea.min.z; z <= probeArea.max.z + 0.01f; z += probeSpacing)
                        pts.Add(new Vector3(x, y, z));
            group.probePositions = pts.ToArray();

            // Sonda de reflexo assada: sem ela, materiais metálicos (dourado, ferro) ficam pretos com luz baked.
            var reflGo = new GameObject("ReflexoAmbiente");
            reflGo.transform.SetParent(root, false);
            reflGo.transform.localPosition = probeArea.center;
            var refl = reflGo.AddComponent<ReflectionProbe>();
            refl.mode = ReflectionProbeMode.Baked;
            refl.size = probeArea.size + Vector3.one;
            refl.boxProjection = true;
            refl.resolution = 128;
            refl.hdr = false;
            // Fundo da captura = ambiente da sala (sem isso entra um "céu" azul padrão e as bordas metálicas ficam azuladas).
            refl.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            refl.backgroundColor = RenderSettings.ambientLight;

            // 4) Configuração do bake (salva ao lado da cena).
            var ls = new LightingSettings
            {
                name = Path.GetFileNameWithoutExtension(scenePath) + "_Iluminacao",
                bakedGI = true,
                realtimeGI = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
                lightmapResolution = texelsPerUnit,
                lightmapMaxSize = maxLightmapSize,
                lightmapPadding = 2,
                directionalityMode = LightmapsMode.NonDirectional,
                lightmapCompression = LightmapCompression.NormalQuality,
                maxBounces = 3,
                indirectScale = 1.1f,
                directSampleCount = 32,
                indirectSampleCount = 256,
                environmentSampleCount = 128,
                filteringMode = LightingSettings.FilterMode.Auto,
                ao = true,
                aoMaxDistance = 1f,
                mixedBakeMode = MixedLightingMode.IndirectOnly,
            };
            string lsPath = Path.ChangeExtension(scenePath, null) + "_Iluminacao.lighting";
            AssetDatabase.DeleteAsset(lsPath);
            AssetDatabase.CreateAsset(ls, lsPath);
            Lightmapping.lightingSettings = ls;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            Debug.Log($"[Somnium] Bake de {Path.GetFileName(scenePath)}: {(ok ? "ok" : "FALHOU")} em {sw.Elapsed.TotalSeconds:0}s " +
                      $"({stat} estáticos, {dyn} dinâmicos, {baked} luzes, {pts.Count} probes, {LightmapSettings.lightmaps.Length} lightmap(s))");
            return ok;
        }

        // ---------------- malhas combinadas

        /// <summary>
        /// Junta muitas cópias de um primitivo (cubo, esfera, cápsula...) numa malha só: um objeto e uma chamada de desenho.
        /// Ideal para hera, grama, tentáculos, elos de corrente. As matrizes são locais ao <paramref name="parent"/>.
        /// </summary>
        public static GameObject Combined(Transform parent, string name, PrimitiveType type, List<Matrix4x4> parts, Material mat)
        {
            var tmp = GameObject.CreatePrimitive(type);
            var primMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            var ci = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++) ci[i] = new CombineInstance { mesh = primMesh, transform = parts[i] };
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(ci, true, true);
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>Material de partícula transparente (URP), com uma textura de bolinha suave gerada.</summary>
        public Material ParticleMat(string name, Color color)
        {
            string texPath = $"{matFolder}/{name}_Particula.png";
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(texPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();

            string path = $"{matFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------------- som sintetizado

        /// <summary>Gera um .wav mono 16 bits a partir de uma função f(t em segundos) → amostra (-1..1), normaliza e importa.</summary>
        public static AudioClip SynthClip(string path, float seconds, System.Func<float, System.Random, float> f, int sampleRate = 44100, int seed = 1)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            int n = (int)(seconds * sampleRate);
            var data = new float[n];
            var rnd = new System.Random(seed);
            float peak = 1e-4f;
            for (int i = 0; i < n; i++)
            {
                data[i] = f((float)i / sampleRate, rnd);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = n * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE")); w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(sampleRate); w.Write(sampleRate * 2);
                w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (var s in data) w.Write((short)Mathf.Clamp(s / peak * 0.85f * 32767f, -32768f, 32767f));
            }
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>AudioSource 3D (som vem da posição do objeto), sem tocar ao iniciar.</summary>
        public static AudioSource Sound3D(GameObject go, AudioClip clip, float volume = 1f, float minDistance = 2f, float maxDistance = 30f)
        {
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = minDistance;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
            s.volume = volume;
            return s;
        }

        // ---------------- projeto

        public static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Mesh SaveMesh(Mesh mesh, string path)
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
    }
}
