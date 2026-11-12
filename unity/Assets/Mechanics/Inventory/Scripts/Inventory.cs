using System;
using System.Collections.Generic;
using UnityEngine;

namespace Somnium.Items
{
    /// <summary>
    /// Inventário do jogador (sem limite de espaço). Existe uma única instância, criada sob demanda
    /// e mantida entre trocas de cena.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        static Inventory instance;

        public static Inventory Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("[Inventory]");
                    instance = go.AddComponent<Inventory>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        readonly List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;

        /// <summary>Disparado sempre que o conteúdo muda.</summary>
        public event Action Changed;

        public bool Has(ItemData item) => item != null && items.Contains(item);

        public void Add(ItemData item)
        {
            if (item == null || items.Contains(item)) return;
            items.Add(item);
            Changed?.Invoke();
        }

        public bool Remove(ItemData item)
        {
            if (!items.Remove(item)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Troca um item por outro na mesma posição (ex.: Livro Antigo vira Livro Aberto).</summary>
        public void Replace(ItemData oldItem, ItemData newItem)
        {
            int i = items.IndexOf(oldItem);
            if (i < 0)
            {
                Add(newItem);
                return;
            }
            if (newItem == null || items.Contains(newItem)) items.RemoveAt(i);
            else items[i] = newItem;
            Changed?.Invoke();
        }

        /// <summary>Tenta combinar dois itens (em qualquer ordem). Os dois são consumidos e o resultado entra no lugar.</summary>
        public bool TryCombine(ItemData a, ItemData b, out ItemData result, out string message)
        {
            result = null;
            message = null;
            if (a == null || b == null || a == b || !Has(a) || !Has(b)) return false;

            var combo = FindCombination(a, b) ?? FindCombination(b, a);
            if (combo == null || combo.result == null) return false;

            int index = Mathf.Min(items.IndexOf(a), items.IndexOf(b));
            items.Remove(a);
            items.Remove(b);
            if (!items.Contains(combo.result))
                items.Insert(Mathf.Clamp(index, 0, items.Count), combo.result);

            result = combo.result;
            message = string.IsNullOrEmpty(combo.message) ? $"Combinou: {combo.result.DisplayName}" : combo.message;
            Changed?.Invoke();
            return true;
        }

        static ItemData.Combination FindCombination(ItemData a, ItemData b)
        {
            foreach (var c in a.combinations)
                if (c != null && c.with == b) return c;
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => instance = null;
    }
}
