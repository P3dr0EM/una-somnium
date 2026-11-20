using System.Collections;
using UnityEngine;

namespace Somnium.Rooms
{
    /// <summary>
    /// Planta carnívora da Estufa. Wither(): encolhe e "desaba" (escala), escurece para marrom (MaterialPropertyBlock,
    /// sem mexer no material compartilhado), apaga o brilho e libera o bloqueio da taça.
    /// SnapWithered(): estado final direto (sala recarregada já resolvida).
    /// </summary>
    public class PlantaMurcha : MonoBehaviour
    {
        [Tooltip("Partes que murcham (encolhem e caem).")]
        public Transform[] parts = new Transform[0];
        [Tooltip("Escala final (relativa) de cada parte: Y bem menor que X/Z = desaba.")]
        public Vector3 witheredScale = new Vector3(0.9f, 0.5f, 0.9f);
        [Tooltip("Renderers que escurecem.")]
        public Renderer[] tint = new Renderer[0];
        public Color witheredColor = new Color(0.22f, 0.16f, 0.1f);
        [Tooltip("Desligados ao murchar (bolsas brilhantes, luz verde, bloqueio da taça).")]
        public GameObject[] disableOnWither = new GameObject[0];
        public float seconds = 3f;
        public AudioSource sound;

        bool done;
        Vector3[] baseScales;

        void Awake()
        {
            baseScales = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++) if (parts[i] != null) baseScales[i] = parts[i].localScale;
        }

        public void Wither()
        {
            if (done) return;
            done = true;
            StartCoroutine(Run());
        }

        public void SnapWithered()
        {
            done = true;
            Apply(1f);
            foreach (var go in disableOnWither) if (go != null) go.SetActive(false);
        }

        IEnumerator Run()
        {
            if (sound != null) sound.Play();
            // O brilho e o bloqueio somem logo no começo (a planta "morre"), o corpo desaba devagar.
            foreach (var go in disableOnWither) if (go != null) go.SetActive(false);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Apply(Mathf.SmoothStep(0f, 1f, t / seconds));
                yield return null;
            }
            Apply(1f);
        }

        void Apply(float k)
        {
            if (baseScales == null) Awake();
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == null) continue;
                var target = Vector3.Scale(baseScales[i], witheredScale);
                parts[i].localScale = Vector3.Lerp(baseScales[i], target, k);
            }
            var block = new MaterialPropertyBlock();
            foreach (var r in tint)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                var baseCol = r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.white;
                block.SetColor("_BaseColor", Color.Lerp(baseCol, witheredColor, k));
                r.SetPropertyBlock(block);
            }
        }
    }
}
