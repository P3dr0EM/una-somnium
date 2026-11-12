using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Somnium.UI
{
    /// <summary>
    /// Cria UI em espaço 3D (world space) por código. No Cardboard, UI "colada na tela" não funciona:
    /// tudo precisa existir no mundo, na frente dos olhos.
    /// </summary>
    public static class UIFactory
    {
        public const int UILayer = 5; // layer "UI" padrão da Unity

        static Font font;
        static Material overlayMaterial;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Material de UI que desenha por cima de tudo (não some atrás de paredes próximas).</summary>
        public static Material OverlayMaterial
        {
            get
            {
                if (overlayMaterial == null)
                {
                    overlayMaterial = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "UI Overlay (ZTest Always)" };
                    overlayMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                }
                return overlayMaterial;
            }
        }

        /// <summary>
        /// Canvas em world space a <paramref name="distance"/> metros do pai.
        /// Escala: 1000 px de largura ocupam "distance" metros (~53° de campo de visão).
        /// </summary>
        public static Canvas CreateWorldCanvas(string name, Transform parent, float distance, Vector2 sizePx, int layer)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.layer = layer;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = sizePx;
            rt.localPosition = new Vector3(0f, 0f, distance);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * (distance / 1000f);
            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor align,
                                      Vector2 pos, Vector2 size, bool overlay = false)
        {
            var rt = CreateRect(parent, name, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (overlay) t.material = OverlayMaterial;

            var shadow = rt.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool overlay = false)
        {
            var rt = CreateRect(parent, name, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (overlay) img.material = OverlayMaterial;
            return img;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
