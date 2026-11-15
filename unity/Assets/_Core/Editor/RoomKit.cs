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
