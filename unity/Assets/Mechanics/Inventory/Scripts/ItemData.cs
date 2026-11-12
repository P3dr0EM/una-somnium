using System;
using System.Collections.Generic;
using UnityEngine;

namespace Somnium.Items
{
    /// <summary>
    /// Definição de um item (asset). Crie em: botão direito > Create > Somnium > Item,
    /// ou use o menu Somnium > Itens > Criar itens do roteiro.
    /// </summary>
    [CreateAssetMenu(menuName = "Somnium/Item", fileName = "Item_Novo")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] string displayName;
        [TextArea(2, 5)] public string description;
        [Tooltip("Ícone opcional para o slot do inventário.")]
        public Sprite icon;

        [Header("Inspeção 3D")]
        [Tooltip("Modelo mostrado no inventário. Use um modelo/prefab simples (scripts e colliders são removidos na exibição). " +
                 "Pode conter InspectHotspots (ex.: a trava escondida na lombada do livro).")]
        public GameObject inspectModel;
        [Tooltip("Maior dimensão do item na inspeção, em metros.")]
        public float inspectSize = 0.22f;

        [Header("Combinações")]
        public List<Combination> combinations = new List<Combination>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        [Serializable]
        public class Combination
        {
            [Tooltip("Item com o qual este combina.")]
            public ItemData with;
            [Tooltip("Item que resulta da combinação (os dois originais são consumidos).")]
            public ItemData result;
            [TextArea] public string message;
        }

#if UNITY_EDITOR
        /// <summary>Usado pelas ferramentas de editor para preencher o item.</summary>
        public void EditorSetup(string newDisplayName, string newDescription)
        {
            displayName = newDisplayName;
            description = newDescription;
        }
#endif
    }
}
