using UnityEngine;

namespace Somnium.Ambiente
{
    /// <summary>
    /// Brilho que "respira" devagar (bioluminescência): varia a emissão dos renderers (MaterialPropertyBlock)
    /// e a intensidade de uma luz opcional. Barato.
    /// </summary>
    public class PulsoBrilho : MonoBehaviour
    {
        public Renderer[] renderers = new Renderer[0];
        public Color emission = new Color(0.4f, 1f, 0.45f);
        public float minIntensity = 0.4f, maxIntensity = 1.6f;
        public float period = 3.2f;
        public Light glowLight;

        float baseLight, offset;
        MaterialPropertyBlock block;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            if (glowLight != null) baseLight = glowLight.intensity;
            offset = Random.value * period;
        }

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin((Time.time + offset) * Mathf.PI * 2f / period);
            float i = Mathf.Lerp(minIntensity, maxIntensity, k);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission * i);
                r.SetPropertyBlock(block);
            }
            if (glowLight != null) glowLight.intensity = baseLight * Mathf.Lerp(0.6f, 1.2f, k);
        }
    }
}
