using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Mostra o progresso de um ItemLock com "Accept Partial" (ex.: as 5 fechaduras da porta principal):
    /// liga os N primeiros objetos de <see cref="insertedVisuals"/> conforme os itens são inseridos.
    /// </summary>
    public class LockProgressIndicator : MonoBehaviour
    {
        public ItemLock target;
        [Tooltip("Um objeto por item (ex.: a chave encaixada em cada fechadura). Começam desligados.")]
        public GameObject[] insertedVisuals = new GameObject[0];

        void OnEnable()
        {
            if (target != null) target.onProgressChanged.AddListener(Show);
        }

        void OnDisable()
        {
            if (target != null) target.onProgressChanged.RemoveListener(Show);
        }

        void Start()
        {
            if (target != null) Show(target.InsertedCount());
        }

        public void Show(int count)
        {
            for (int i = 0; i < insertedVisuals.Length; i++)
                if (insertedVisuals[i] != null) insertedVisuals[i].SetActive(i < count);
        }
    }
}
