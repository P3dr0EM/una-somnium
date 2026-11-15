using System.Collections;
using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Gira este objeto até <see cref="openLocalEuler"/> quando chamado (ex.: tampa do alçapão que abre ao cortar a corrente).
    /// Ligue Play() no "On Unlocked" de um ItemLock e SnapOpen() no "On Restored Unlocked" (sala recarregada já aberta).
    /// </summary>
    public class GirarAoAtivar : MonoBehaviour
    {
        [Tooltip("Rotação local final (graus).")]
        public Vector3 openLocalEuler = new Vector3(-110f, 0f, 0f);
        public float seconds = 0.9f;
        [Tooltip("Espera antes de começar (ex.: deixar a mensagem aparecer).")]
        public float delay = 0.2f;

        public void Play()
        {
            StopAllCoroutines();
            StartCoroutine(Run());
        }

        public void SnapOpen() => transform.localRotation = Quaternion.Euler(openLocalEuler);

        IEnumerator Run()
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var from = transform.localRotation;
            var to = Quaternion.Euler(openLocalEuler);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                k = 1f - (1f - k) * (1f - k); // desacelera no fim
                transform.localRotation = Quaternion.Slerp(from, to, k);
                yield return null;
            }
            transform.localRotation = to;
        }
    }
}
