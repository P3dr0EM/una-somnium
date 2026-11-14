using System.Collections.Generic;
using Assets.Scripts;
using Somnium.Core;
using Somnium.Items;
using Somnium.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Somnium.Interaction
{
    /// <summary>
    /// Algo que precisa de item(ns) para funcionar. Se o jogador tem o item, interagir já usa (uso automático).
    /// Exemplos do roteiro:
    ///  - Corrente do alçapão: requer Alicate, desativa a corrente.
    ///  - Mecanismo do Salão Invertido: requer Manivela Quadrada, onUnlocked derruba o lustre.
    ///  - Galão de ácido: requer Cartucho Vazio, recompensa Herbicida.
    ///  - Porta Principal: requer as 4 chaves com "acceptPartial" (vai inserindo e mostra 3/4).
    /// O estado fica salvo no WorldState: se a sala for descarregada e recarregada, continua destravado.
    /// </summary>
    public class ItemLock : Interactable
    {
        [Header("Requisitos")]
        public List<ItemData> requiredItems = new List<ItemData>();
        [Tooltip("Remove os itens do inventário ao usar.")]
        public bool consumeItems = true;
        [Tooltip("Aceita os itens aos poucos (ex.: porta com 5 fechaduras).")]
        public bool acceptPartial;

        [Header("Textos")]
        [TextArea] public string lockedMessage = "Não consigo fazer nada com isto agora.";
        [TextArea] public string unlockedMessage = "";
        [TextArea] public string alreadyUnlockedMessage = "";
        [Tooltip("Dica quando o jogador tem o item (ex.: 'Dar ração'). Vazio = 'Usar <item>'.")]
        public string usablePromptText = "";

        [Header("Resultado")]
        [Tooltip("Item recebido ao destravar (ex.: Herbicida).")]
        public ItemData rewardItem;
        [Tooltip("Itens extras recebidos ao destravar (ex.: o gato dá a Chave 1 e a Metade do Medalhão).")]
        public ItemData[] extraRewardItems = new ItemData[0];
        public GameObject[] activateOnUnlock = new GameObject[0];
        public GameObject[] deactivateOnUnlock = new GameObject[0];
        [Tooltip("Depois de destravar, a mira passa a ignorar este objeto.")]
        public bool disableAfterUnlock = true;
        public UnityEvent onUnlocked = new UnityEvent();
        [Tooltip("Chamado no Start quando já estava destravado (sala recarregada). Use para pôr o estado final sem animação.")]
        public UnityEvent onRestoredUnlocked = new UnityEvent();
        [Tooltip("Com Accept Partial: chamado com quantos itens já foram inseridos (também no Start). Ex.: acender fechaduras.")]
        public UnityEvent<int> onProgressChanged = new UnityEvent<int>();

        [Header("Save")]
        [Tooltip("Id único. Vazio = cena + caminho na hierarquia.")]
        public string saveId;

        bool unlocked;

        public bool IsUnlocked => unlocked;

        string Key => WorldState.KeyFor(this, "lock", saveId);
        string PartKey(ItemData item) => Key + "/" + item.name;
        bool IsInserted(ItemData item) => item != null && WorldState.Has(PartKey(item));

        public override bool CanInteract => base.CanInteract && !(unlocked && disableAfterUnlock);

        public override string Prompt
        {
            get
            {
                var usable = FirstUsableItem();
                if (usable != null) return string.IsNullOrEmpty(usablePromptText) ? $"Usar {usable.DisplayName}" : usablePromptText;
                return string.IsNullOrEmpty(promptText) ? "Examinar" : promptText;
            }
        }

        void Start()
        {
            if (acceptPartial) onProgressChanged.Invoke(InsertedCount());
            if (!WorldState.Has(Key)) return;
            unlocked = true;
            ApplyUnlockedState();
            onRestoredUnlocked.Invoke();
        }

        public override void Interact()
        {
            var hud = HUD.Instance;
            if (unlocked)
            {
                if (!string.IsNullOrEmpty(alreadyUnlockedMessage)) hud.ShowMessage(alreadyUnlockedMessage);
                return;
            }

            if (requiredItems.Count == 0)
            {
                Unlock();
                return;
            }

            var inv = Inventory.Instance;

            if (acceptPartial)
            {
                var used = new List<string>();
                foreach (var item in requiredItems)
                {
                    if (item == null || IsInserted(item) || !inv.Has(item)) continue;
                    if (consumeItems) inv.Remove(item);
                    WorldState.Set(PartKey(item));
                    used.Add(item.DisplayName);
                }

                int inserted = InsertedCount();
                if (used.Count > 0) onProgressChanged.Invoke(inserted);
                if (inserted >= requiredItems.Count)
                {
                    Unlock();
                    return;
                }

                string progress = $"({inserted}/{requiredItems.Count})";
                hud.ShowMessage(used.Count > 0 ? $"Usou {string.Join(", ", used)} {progress}" : $"{lockedMessage} {progress}");
                return;
            }

            foreach (var item in requiredItems)
            {
                if (!inv.Has(item))
                {
                    hud.ShowMessage(lockedMessage);
                    return;
                }
            }

            if (consumeItems)
                foreach (var item in requiredItems) inv.Remove(item);
            Unlock();
        }

        void Unlock()
        {
            unlocked = true;
            WorldState.Set(Key);
            var rewards = new List<string>();
            if (rewardItem != null) rewards.Add(rewardItem.DisplayName);
            foreach (var extra in extraRewardItems)
                if (extra != null) rewards.Add(extra.DisplayName);
            if (rewardItem != null) Inventory.Instance.Add(rewardItem);
            foreach (var extra in extraRewardItems)
                if (extra != null) Inventory.Instance.Add(extra);

            ApplyUnlockedState();

            string msg = unlockedMessage;
            if (rewards.Count > 0)
            {
                string got = $"Obteve: {string.Join(", ", rewards)}";
                msg = string.IsNullOrEmpty(msg) ? got : msg + "\n" + got;
            }
            if (!string.IsNullOrEmpty(msg)) HUD.Instance.ShowMessage(msg);

            onUnlocked.Invoke();
        }

        void ApplyUnlockedState()
        {
            foreach (var go in activateOnUnlock) if (go != null) go.SetActive(true);
            foreach (var go in deactivateOnUnlock) if (go != null) go.SetActive(false);
        }

        /// <summary>Item que seria usado agora (para a dica "Usar X"), ou null.</summary>
        ItemData FirstUsableItem()
        {
            if (unlocked || requiredItems.Count == 0) return null;
            var inv = Inventory.Instance;

            if (acceptPartial)
            {
                foreach (var item in requiredItems)
                    if (item != null && !IsInserted(item) && inv.Has(item)) return item;
                return null;
            }

            foreach (var item in requiredItems)
                if (!inv.Has(item)) return null;
            return requiredItems[0];
        }

        /// <summary>Quantos dos itens exigidos já foram inseridos (Accept Partial).</summary>
        public int InsertedCount()
        {
            int n = 0;
            foreach (var item in requiredItems)
                if (IsInserted(item)) n++;
            return n;
        }
    }
}
