using UnityEngine;

namespace Somnium.Ambiente
{
    /// <summary>Balança este objeto em volta do eixo Z local (pêndulo). No Salão Invertido o pêndulo aponta para cima.</summary>
    public class PenduloInvertido : MonoBehaviour
    {
        [Tooltip("Amplitude em graus.")]
        public float amplitude = 14f;
        [Tooltip("Período de um vaivém completo, em segundos.")]
        public float period = 2f;

        Quaternion rest;

        void Awake() => rest = transform.localRotation;

        void Update()
        {
            float a = Mathf.Sin(Time.time * Mathf.PI * 2f / period) * amplitude;
            transform.localRotation = rest * Quaternion.Euler(0f, 0f, a);
        }
    }
}
