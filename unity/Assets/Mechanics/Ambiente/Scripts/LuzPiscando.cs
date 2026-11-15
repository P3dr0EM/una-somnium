using UnityEngine;

namespace Somnium.Ambiente
{
    /// <summary>
    /// Lâmpada fluorescente com defeito: fica acesa e, de tempos em tempos, pisca algumas vezes.
    /// Liga/desliga a Light e troca o material do painel (aceso/apagado). Opcionalmente toca um zumbido.
    /// </summary>
    public class LuzPiscando : MonoBehaviour
    {
        public Light target;
        public Renderer panel;
        public Material onMaterial;
        public Material offMaterial;

        [Tooltip("Intervalo aleatório entre crises de pisca-pisca (s).")]
        public Vector2 interval = new Vector2(3f, 8f);
        [Tooltip("Quantas piscadas por crise.")]
        public Vector2Int flickers = new Vector2Int(2, 6);

        float baseIntensity;
        float nextBurst;
        int remaining;
        float nextToggle;
        bool on = true;

        void Awake()
        {
            if (target != null) baseIntensity = target.intensity;
            nextBurst = Time.time + Random.Range(interval.x, interval.y);
        }

        void Update()
        {
            if (remaining == 0)
            {
                if (Time.time < nextBurst) return;
                remaining = Random.Range(flickers.x, flickers.y + 1) * 2; // apaga + acende
                nextToggle = Time.time;
            }

            if (Time.time < nextToggle) return;
            Set(!on);
            remaining--;
            nextToggle = Time.time + Random.Range(0.03f, 0.14f);
            if (remaining == 0)
            {
                Set(true);
                nextBurst = Time.time + Random.Range(interval.x, interval.y);
            }
        }

        void Set(bool value)
        {
            on = value;
            if (target != null) target.intensity = value ? baseIntensity : baseIntensity * 0.08f;
            if (panel != null && onMaterial != null && offMaterial != null) panel.sharedMaterial = value ? onMaterial : offMaterial;
        }
    }
}
