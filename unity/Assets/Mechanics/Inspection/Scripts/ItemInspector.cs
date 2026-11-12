using Somnium.Core;
using Somnium.Items;
using Somnium.UI;
using UnityEngine;

namespace Somnium.Inspection
{
    /// <summary>
    /// Mostra o modelo 3D de um item flutuando à frente do jogador e permite girá-lo.
    /// Usado pelo InventoryUI. Remove scripts, colliders e rigidbodies da cópia exibida
    /// (mantém só os InspectHotspots).
    /// </summary>
    public class ItemInspector
    {
        readonly Transform pivot;
        readonly int layer;
        GameObject holder;
        InspectHotspot[] hotspots = new InspectHotspot[0];

        public ItemData Current { get; private set; }

        public ItemInspector(Transform parent, Vector3 localPosition, int layer)
        {
            this.layer = layer;
            var go = new GameObject("InspectPivot");
            go.layer = layer;
            pivot = go.transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
        }

        public static string HotspotKey(ItemData item, InspectHotspot h) => $"inspect:{item.name}:{h.id}";

        public void Show(ItemData item)
        {
            Clear();
            Current = item;
            if (item == null) return;

            pivot.localRotation = Quaternion.Euler(15f, -25f, 0f);

            // Monta a cópia desativada, para nenhum Awake/Start dos scripts do modelo rodar.
            holder = new GameObject("InspectHolder");
            holder.SetActive(false);
            holder.layer = layer;

            GameObject model;
            if (item.inspectModel != null)
            {
                model = Object.Instantiate(item.inspectModel, holder.transform);
            }
            else
            {
                model = GameObject.CreatePrimitive(PrimitiveType.Cube); // sem modelo: placeholder
                model.transform.SetParent(holder.transform, false);
            }
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(mb is InspectHotspot)) Object.DestroyImmediate(mb);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (var col in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            UIFactory.SetLayerRecursively(holder, layer);

            holder.transform.SetParent(pivot, false);
            holder.SetActive(true);

            // Normaliza tamanho e centraliza no pivô.
            if (TryGetBounds(holder, out var bounds))
            {
                float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (maxDim > 1e-4f) holder.transform.localScale = Vector3.one * (item.inspectSize / maxDim);
                if (TryGetBounds(holder, out bounds))
                    holder.transform.position += pivot.position - bounds.center;
            }

            hotspots = holder.GetComponentsInChildren<InspectHotspot>(true);
        }

        public void Clear()
        {
            Current = null;
            hotspots = new InspectHotspot[0];
            if (holder != null) Object.Destroy(holder);
            holder = null;
        }

        /// <summary>Gira o item em relação à visão: x = horizontal, y = vertical (graus).</summary>
        public void Rotate(Vector2 degrees, Transform view)
        {
            if (holder == null) return;
            pivot.Rotate(view.up, -degrees.x, Space.World);
            pivot.Rotate(view.right, degrees.y, Space.World);
        }

        /// <summary>Hotspot ainda não usado que está virado para o jogador, ou null.</summary>
        public InspectHotspot FacingHotspot(Vector3 viewerPosition)
        {
            if (Current == null) return null;
            foreach (var h in hotspots)
            {
                if (h == null || WorldState.Has(HotspotKey(Current, h))) continue;
                if (h.IsFacing(viewerPosition)) return h;
            }
            return null;
        }

        static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool found = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!found) { bounds = r.bounds; found = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return found;
        }
    }
}
