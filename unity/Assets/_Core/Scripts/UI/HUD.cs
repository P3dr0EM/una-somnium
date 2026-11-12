using UnityEngine;
using UnityEngine.UI;

namespace Somnium.UI
{
    /// <summary>
    /// HUD mínimo em world space, preso à câmera: mira, dica de interação ("[E] Abrir porta")
    /// e mensagens curtas ("Obteve: Alicate"). É criado automaticamente na primeira vez que é usado.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        const float Distance = 1.0f;

        static HUD instance;

        Canvas canvas;
        Image reticle;
        Text prompt;
        Text message;
        float messageUntil;
        string currentPrompt;

        public static bool Exists => instance != null;

        /// <summary>Instância atual; cria presa à Camera.main se ainda não existir.</summary>
        public static HUD Instance => instance != null ? instance : EnsureFor(Camera.main);

        public static HUD EnsureFor(Camera cam)
        {
            if (instance != null) return instance;
            var go = new GameObject("[HUD]");
            go.layer = UIFactory.UILayer;
            if (cam != null) go.transform.SetParent(cam.transform, false);
            else Debug.LogWarning("[HUD] Nenhuma câmera encontrada; HUD criado solto na cena.");
            instance = go.AddComponent<HUD>();
            instance.Build();
            return instance;
        }

        void Build()
        {
            canvas = UIFactory.CreateWorldCanvas("HUDCanvas", transform, Distance, new Vector2(1000f, 1000f), UIFactory.UILayer);
            var root = canvas.transform;

            reticle = UIFactory.CreateImage(root, "Mira", Vector2.zero, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.6f), overlay: true);
            prompt = UIFactory.CreateText(root, "Dica", 30, TextAnchor.UpperCenter, new Vector2(0f, -60f), new Vector2(800f, 60f), overlay: true);
            message = UIFactory.CreateText(root, "Mensagem", 30, TextAnchor.MiddleCenter, new Vector2(0f, -230f), new Vector2(800f, 120f), overlay: true);
            message.color = new Color(1f, 0.92f, 0.6f);

            prompt.text = "";
            message.text = "";
        }

        /// <summary>Texto ao mirar em algo interagível. Null ou vazio esconde.</summary>
        public void SetPrompt(string text)
        {
            if (text == currentPrompt) return;
            currentPrompt = text;
            prompt.text = text ?? "";
        }

        /// <summary>A mira cresce quando há algo interagível na frente.</summary>
        public void SetFocused(bool focused)
        {
            float size = focused ? 12f : 6f;
            reticle.rectTransform.sizeDelta = new Vector2(size, size);
            reticle.color = focused ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.6f);
        }

        public void ShowMessage(string text, float seconds = 3f)
        {
            message.text = text ?? "";
            messageUntil = Time.unscaledTime + seconds;
        }

        public void SetVisible(bool visible) => canvas.gameObject.SetActive(visible);

        void LateUpdate()
        {
            if (message.text.Length > 0 && Time.unscaledTime > messageUntil) message.text = "";

            // Se o HUD nasceu sem câmera (ex.: cena carregou antes do player), prende na câmera assim que existir.
            if (transform.parent == null && Camera.main != null)
                transform.SetParent(Camera.main.transform, false);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
