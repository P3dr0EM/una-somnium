using UnityEngine;

namespace Somnium.Ambiente
{
    /// <summary>
    /// Fogo tremulando: varia a intensidade de uma luz de forma suave (ruído) e, opcionalmente,
    /// "respira" a escala das chamas. Barato: sem partículas.
    /// </summary>
    public class FogoTremulo : MonoBehaviour
    {
        public Light target;
        [Tooltip("Chamas que pulsam de tamanho junto com a luz.")]
        public Transform[] flames = new Transform[0];
        [Range(0f, 1f)] public float variation = 0.35f;
        public float speed = 6f;

        float baseIntensity;
        Vector3[] baseScales;
        float seed;

        void Awake()
        {
            if (target != null) baseIntensity = target.intensity;
            baseScales = new Vector3[flames.Length];
            for (int i = 0; i < flames.Length; i++) if (flames[i] != null) baseScales[i] = flames[i].localScale;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed);
            float k = 1f + (n - 0.5f) * 2f * variation;
            if (target != null) target.intensity = baseIntensity * k;
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null) continue;
                float f = Mathf.PerlinNoise(seed + i * 3.7f, Time.time * speed * 1.3f);
                flames[i].localScale = new Vector3(baseScales[i].x, baseScales[i].y * (0.8f + f * 0.45f), baseScales[i].z);
            }
        }
    }
}
