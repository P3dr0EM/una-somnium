using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Um "brilho" que pisca de tempos em tempos (escala sobe e desce), para chamar atenção para um item
    /// escondido sem deixá-lo óbvio (ex.: a Manivela na grama). Coloque num objeto filho pequeno e emissivo.
    /// </summary>
    public class Brilho : MonoBehaviour
    {
        [Tooltip("Intervalo entre os brilhos, em segundos.")]
        public float interval = 2.6f;
        [Tooltip("Duração de cada brilho, em segundos.")]
        public float duration = 0.45f;
        public float maxScale = 1f;

        float offset;

        void Awake()
        {
            offset = Random.value * interval;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            float t = (Time.time + offset) % interval;
            float k = t < duration ? Mathf.Sin(t / duration * Mathf.PI) : 0f;
            transform.localScale = Vector3.one * (k * maxScale);
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position) * Quaternion.Euler(0f, 0f, 45f);
        }
    }
}
