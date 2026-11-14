using System.Collections.Generic;
using Somnium.Core;
using Somnium.Inspection;
using Somnium.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Somnium.Items
{
    /// <summary>
    /// Painel de inventário flutuante (estilo Resident Evil), funciona no PC e no Cardboard.
    ///  - Abre/fecha: Tab / I / Y. Voltar: Esc / B.
    ///  - Navegar: WASD / setas / analógico esquerdo / D-pad.
    ///  - Girar o item: mouse / analógico direito.
    ///  - Examinar segredo (InspectHotspot virado para você): E / clique / A.
    ///  - Combinar: Q / X no primeiro item, depois Q / X (ou E / A) no segundo.
    /// Com o painel aberto o jogo pausa e a câmera passa a mostrar só o painel e o item
    /// (assim paredes próximas não cobrem o item).
    /// Adicionado automaticamente pelo PlayerInputLogic.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] Camera viewCamera;
        [Tooltip("Distância do painel até os olhos, em metros.")]
        public float panelDistance = 0.8f;
        public Color backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        [Tooltip("Velocidade de giro do item com o mouse (graus por pixel).")]
        public float mouseRotateSpeed = 0.3f;
        [Tooltip("Velocidade de giro do item com o analógico (graus por segundo).")]
        public float stickRotateSpeed = 180f;

        const int Columns = 3;
        const int VisibleRows = 4;
        const float SlotW = 140f, SlotH = 100f, SlotGap = 10f;
        static readonly Color SlotNormal = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color SlotSelected = new Color(1f, 0.85f, 0.4f, 0.45f);
        static readonly Color SlotCombine = new Color(0.4f, 0.8f, 1f, 0.45f);

        bool open;
        int selected;
        ItemData combineSource;

        Transform root;
        Canvas canvas;
        RectTransform gridArea;
        readonly List<Image> slotBackgrounds = new List<Image>();
        readonly List<Text> slotLabels = new List<Text>();
        readonly List<Image> slotIcons = new List<Image>();
        Text nameText, descText, feedbackText, hintText;
        ItemInspector inspector;
        Light inspectLight;

        int savedCullingMask;
        CameraClearFlags savedClearFlags;
        Color savedBackground;
        float savedTimeScale = 1f;

        bool navHeld;
        float navCooldown;
        float feedbackUntil;

        public bool IsOpen => open;

        public void Setup(Camera cam) => viewCamera = cam;

        void Update()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            if (!open)
            {
                if (!GameState.InMenu && !GameState.MenuClosedThisFrame && GameInput.InventoryPressed) Open();
                return;
            }

            if (GameInput.InventoryPressed || (GameInput.BackPressed && combineSource == null))
            {
                Close();
                return;
            }
            if (GameInput.BackPressed)
            {
                combineSource = null;
                Feedback("");
                Refresh();
                return;
            }

            HandleNavigation();
            inspector.Rotate(GameInput.LookDegrees(mouseRotateSpeed, stickRotateSpeed), viewCamera.transform);
            HandleActions();
            UpdateHints();

            if (feedbackText.text.Length > 0 && Time.unscaledTime > feedbackUntil) feedbackText.text = "";
        }

        // ---------------- Abrir / fechar ----------------

        void Open()
        {
            if (root == null) Build();

            open = true;
            GameState.InMenu = true;
            combineSource = null;
            navHeld = false;

            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // A câmera passa a ver só a layer de UI, com fundo escuro.
            savedCullingMask = viewCamera.cullingMask;
            savedClearFlags = viewCamera.clearFlags;
            savedBackground = viewCamera.backgroundColor;
            viewCamera.cullingMask = 1 << UIFactory.UILayer;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = backgroundColor;

            // O painel fica parado no mundo, na frente de onde o jogador olha (no VR, painel preso à cabeça enjoa).
            Vector3 forward = viewCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = viewCamera.transform.up;
            root.SetPositionAndRotation(viewCamera.transform.position, Quaternion.LookRotation(forward.normalized, Vector3.up));
            root.gameObject.SetActive(true);

            if (HUD.Exists) HUD.Instance.SetVisible(false);

            Inventory.Instance.Changed += Refresh;
            Feedback("");
            Refresh();
        }

        void Close()
        {
            if (!open) return;
            open = false;
            GameState.InMenu = false;
            GameState.MarkMenuClosed();
            Time.timeScale = savedTimeScale;

            if (viewCamera != null)
            {
                viewCamera.cullingMask = savedCullingMask;
                viewCamera.clearFlags = savedClearFlags;
                viewCamera.backgroundColor = savedBackground;
            }

            inspector.Clear();
            root.gameObject.SetActive(false);
            if (HUD.Exists) HUD.Instance.SetVisible(true);
            Inventory.Instance.Changed -= Refresh;
        }

        void OnDisable() => Close();

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        // ---------------- Construção da UI ----------------

        void Build()
        {
            var rootGo = new GameObject("[InventoryRoot]");
            rootGo.layer = UIFactory.UILayer;
            // Objetos criados sem pai nascem na cena ATIVA (a sala). Ao trocar de sala ela é descarregada e levaria
            // a UI junto (MissingReferenceException). Fica na mesma cena do Player (persistencia).
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootGo, gameObject.scene);
            root = rootGo.transform;

            canvas = UIFactory.CreateWorldCanvas("InventoryCanvas", root, panelDistance, new Vector2(1000f, 640f), UIFactory.UILayer);
            var c = canvas.transform;

            UIFactory.CreateImage(c, "Fundo", Vector2.zero, new Vector2(1000f, 640f), new Color(0.06f, 0.07f, 0.09f, 0.94f));
            var title = UIFactory.CreateText(c, "Titulo", 44, TextAnchor.MiddleCenter, new Vector2(0f, 280f), new Vector2(900f, 60f));
            title.text = "INVENTÁRIO";
            title.fontStyle = FontStyle.Bold;

            gridArea = UIFactory.CreateRect(c, "Grade", new Vector2(-250f, 0f), new Vector2(Columns * (SlotW + SlotGap), VisibleRows * (SlotH + SlotGap)));

            nameText = UIFactory.CreateText(c, "Nome", 34, TextAnchor.MiddleCenter, new Vector2(240f, 215f), new Vector2(460f, 50f));
            nameText.fontStyle = FontStyle.Bold;
            descText = UIFactory.CreateText(c, "Descricao", 24, TextAnchor.UpperCenter, new Vector2(240f, -175f), new Vector2(460f, 100f));
            feedbackText = UIFactory.CreateText(c, "Retorno", 24, TextAnchor.MiddleCenter, new Vector2(240f, -240f), new Vector2(460f, 40f));
            feedbackText.color = new Color(1f, 0.85f, 0.4f);
            hintText = UIFactory.CreateText(c, "Dicas", 21, TextAnchor.MiddleCenter, new Vector2(0f, -295f), new Vector2(980f, 36f));
            hintText.color = new Color(0.75f, 0.75f, 0.8f);

            // O item fica na metade direita, um pouco à frente do painel.
            float itemDepth = panelDistance * 0.65f;
            float itemX = 240f / 1000f * panelDistance * (itemDepth / panelDistance);
            inspector = new ItemInspector(root, new Vector3(itemX, 0.01f, itemDepth), UIFactory.UILayer);

            var lightGo = new GameObject("InspectLight");
            lightGo.layer = UIFactory.UILayer;
            lightGo.transform.SetParent(root, false);
            lightGo.transform.localPosition = new Vector3(0.3f, 0.35f, 0.1f);
            inspectLight = lightGo.AddComponent<Light>();
            inspectLight.type = LightType.Point;
            inspectLight.range = 2f;
            inspectLight.intensity = 2f;
            inspectLight.cullingMask = 1 << UIFactory.UILayer;

            root.gameObject.SetActive(false);
        }

        void EnsureSlots(int count)
        {
            while (slotBackgrounds.Count < count)
            {
                int i = slotBackgrounds.Count;
                int col = i % Columns, row = i / Columns;
                float x = -gridArea.sizeDelta.x / 2f + SlotW / 2f + col * (SlotW + SlotGap);
                float y = gridArea.sizeDelta.y / 2f - SlotH / 2f - row * (SlotH + SlotGap);

                var bg = UIFactory.CreateImage(gridArea, $"Slot{i}", new Vector2(x, y), new Vector2(SlotW, SlotH), SlotNormal);
                var icon = UIFactory.CreateImage(bg.transform, "Icone", new Vector2(0f, 12f), new Vector2(56f, 56f), Color.white);
                icon.preserveAspect = true;
                var label = UIFactory.CreateText(bg.transform, "Nome", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(SlotW - 10f, SlotH - 10f));

                slotBackgrounds.Add(bg);
                slotIcons.Add(icon);
                slotLabels.Add(label);
            }
        }

        // ---------------- Atualização ----------------

        void Refresh()
        {
            var items = Inventory.Instance.Items;
            selected = items.Count == 0 ? 0 : Mathf.Clamp(selected, 0, items.Count - 1);

            int maxVisible = Columns * VisibleRows;
            EnsureSlots(Mathf.Min(Mathf.Max(items.Count, 1), maxVisible));
            int firstRow = Mathf.Max(0, selected / Columns - (VisibleRows - 1));

            for (int s = 0; s < slotBackgrounds.Count; s++)
            {
                int i = firstRow * Columns + s;
                bool used = i < items.Count && s < maxVisible;
                slotBackgrounds[s].gameObject.SetActive(used);
                if (!used) continue;

                var item = items[i];
                slotBackgrounds[s].color = i == selected ? SlotSelected : (item == combineSource ? SlotCombine : SlotNormal);

                bool hasIcon = item.icon != null;
                slotIcons[s].gameObject.SetActive(hasIcon);
                if (hasIcon) slotIcons[s].sprite = item.icon;

                slotLabels[s].text = item.DisplayName;
                slotLabels[s].alignment = hasIcon ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter;
            }

            var current = items.Count > 0 ? items[selected] : null;
            nameText.text = current != null ? current.DisplayName : "";
            descText.text = current != null ? current.description : "Nenhum item.";
            if (inspector.Current != current) inspector.Show(current);
        }

        void HandleNavigation()
        {
            Vector2 nav = GameInput.Navigate;
            if (nav.magnitude < 0.5f)
            {
                navHeld = false;
                navCooldown = 0f;
                return;
            }

            navCooldown -= Time.unscaledDeltaTime;
            if (navCooldown > 0f) return;
            navCooldown = navHeld ? 0.15f : 0.35f; // primeira repetição mais lenta
            navHeld = true;

            int count = Inventory.Instance.Items.Count;
            if (count == 0) return;

            int step = Mathf.Abs(nav.x) > Mathf.Abs(nav.y)
                ? (nav.x > 0f ? 1 : -1)
                : (nav.y > 0f ? -Columns : Columns);
            int next = selected + step;
            if (next < 0 || next >= count) return;

            selected = next;
            Refresh();
        }

        void HandleActions()
        {
            var inv = Inventory.Instance;
            if (inv.Items.Count == 0) return;
            var item = inv.Items[selected];

            bool combinePressed = GameInput.CombinePressed;
            bool interactPressed = GameInput.InteractPressed;

            if (combinePressed || (combineSource != null && interactPressed))
            {
                if (combineSource == null)
                {
                    combineSource = item;
                    Feedback($"Combinar {item.DisplayName} com...", 60f);
                }
                else if (combineSource == item)
                {
                    combineSource = null;
                    Feedback("");
                }
                else
                {
                    var source = combineSource;
                    combineSource = null;
                    if (inv.TryCombine(source, item, out var result, out var msg))
                    {
                        selected = IndexOf(result);
                        Feedback(msg);
                    }
                    else
                    {
                        Feedback("Isso não combina.");
                    }
                }
                Refresh();
                return;
            }

            if (interactPressed)
            {
                var hotspot = inspector.FacingHotspot(viewCamera.transform.position);
                if (hotspot == null) return;

                WorldState.Set(ItemInspector.HotspotKey(item, hotspot));
                if (hotspot.rewardItem != null) inv.Add(hotspot.rewardItem);
                if (hotspot.transformsInto != null) inv.Replace(item, hotspot.transformsInto);

                string msg = hotspot.message;
                if (string.IsNullOrEmpty(msg) && hotspot.rewardItem != null) msg = $"Obteve: {hotspot.rewardItem.DisplayName}";
                Feedback(msg ?? "");
            }
        }

        void UpdateHints()
        {
            string hints = $"[{GameInput.NavigateLabel}] Navegar   [{GameInput.RotateLabel}] Girar   " +
                           $"[{GameInput.CombineLabel}] Combinar   [{GameInput.InventoryLabel}] Fechar";

            var hotspot = combineSource == null && viewCamera != null ? inspector.FacingHotspot(viewCamera.transform.position) : null;
            if (hotspot != null) hints = $"[{GameInput.InteractLabel}] {hotspot.prompt}      " + hints;
            if (combineSource != null) hints = $"Escolha o 2º item: [{GameInput.CombineLabel}] confirmar   [{GameInput.BackLabel}] cancelar";

            hintText.text = hints;
        }

        void Feedback(string text, float seconds = 3f)
        {
            if (feedbackText == null) return;
            feedbackText.text = text;
            feedbackUntil = Time.unscaledTime + seconds;
        }

        int IndexOf(ItemData item)
        {
            var items = Inventory.Instance.Items;
            for (int i = 0; i < items.Count; i++)
                if (items[i] == item) return i;
            return 0;
        }
    }
}
