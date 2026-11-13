using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Somnium.UI
{
    /// <summary>
    /// Fade de tela inteira (cobre o campo de visão também no Cardboard) com texto central opcional.
    /// Usa tempo não escalado: funciona com o jogo pausado.
    /// Ex.: yield return ScreenFader.Instance.FadeTo(Color.white, 1f, 3f);
    /// </summary>
    public class ScreenFader : MonoBehaviour
    {
        static ScreenFader instance;

        Image overlay;
        Text centerText;

        public static ScreenFader Instance => instance != null ? instance : Create(Camera.main);

        static ScreenFader Create(Camera cam)
        {
            var go = new GameObject("[ScreenFader]");
            go.layer = UIFactory.UILayer;
            instance = go.AddComponent<ScreenFader>();

            // Logo depois do plano de corte da câmera e bem largo (~140°), para cobrir a visão de cada olho.
            float distance = cam != null ? cam.nearClipPlane + 0.05f : 0.35f;
            if (cam != null) go.transform.SetParent(cam.transform, false);
            var canvas = UIFactory.CreateWorldCanvas("FaderCanvas", go.transform, distance, new Vector2(6000f, 6000f), UIFactory.UILayer);
            canvas.sortingOrder = 1000;

            instance.overlay = UIFactory.CreateImage(canvas.transform, "Overlay", Vector2.zero, new Vector2(6000f, 6000f), new Color(0, 0, 0, 0), overlay: true);
            // Texto em escala própria: o canvas é enorme, então o texto precisa de fonte grande.
            instance.centerText = UIFactory.CreateText(canvas.transform, "Texto", 160, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(3000f, 600f), overlay: true);
            instance.centerText.color = new Color(0.1f, 0.1f, 0.12f, 0f);
            return instance;
        }

        /// <summary>Anima a cor/transparência do overlay até <paramref name="alpha"/> em <paramref name="seconds"/>.</summary>
        public IEnumerator FadeTo(Color color, float alpha, float seconds)
        {
            Color from = overlay.color;
            Color to = new Color(color.r, color.g, color.b, alpha);
            if (from.a <= 0.001f) from = new Color(color.r, color.g, color.b, 0f);

            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                overlay.color = Color.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            overlay.color = to;
        }

        public IEnumerator ShowText(string text, Color color, float fadeSeconds)
        {
            centerText.text = text;
            Color from = new Color(color.r, color.g, color.b, 0f);
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                centerText.color = Color.Lerp(from, color, t / fadeSeconds);
                yield return null;
            }
            centerText.color = color;
        }

        public void Clear()
        {
            overlay.color = new Color(0, 0, 0, 0);
            centerText.text = "";
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
