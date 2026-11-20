using System.Collections;
using UnityEngine;

namespace Somnium.Rooms
{
    /// <summary>
    /// Painel de irrigação da Estufa. Ligue Play() no "On Unlocked" do ItemLock (Herbicida no tanque)
    /// e Snap() no "On Restored Unlocked". Liga os sprinklers (névoa) por alguns segundos e faz a planta murchar.
    /// </summary>
    public class Irrigacao : MonoBehaviour
    {
        public ParticleSystem[] sprinklers = new ParticleSystem[0];
        public PlantaMurcha plant;
        [Tooltip("Tempo entre ligar a névoa e a planta começar a murchar.")]
        public float witherDelay = 1.6f;
        public float sprayDuration = 5f;
        public AudioSource hiss;
        [Tooltip("Ponteiro do manômetro do painel (gira ao abrir o registro).")]
        public Transform gaugeNeedle;

        public void Play() => StartCoroutine(Run());

        public void Snap()
        {
            if (plant != null) plant.SnapWithered();
            if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, -120f);
        }

        IEnumerator Run()
        {
            if (hiss != null) hiss.Play();
            foreach (var s in sprinklers) if (s != null) s.Play();
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(60f, -120f, t / 0.6f));
                yield return null;
            }
            yield return new WaitForSeconds(Mathf.Max(0f, witherDelay - 0.6f));
            if (plant != null) plant.Wither();
            yield return new WaitForSeconds(Mathf.Max(0f, sprayDuration - witherDelay));
            foreach (var s in sprinklers) if (s != null) s.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (hiss != null) hiss.Stop();
        }
    }
}
