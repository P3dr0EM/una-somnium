using System.Collections;
using UnityEngine;

namespace Somnium.Rooms
{
    /// <summary>
    /// Lustre do Salão Invertido. Ligue Fall() no "On Unlocked" do ItemLock do mecanismo (Manivela Quadrada)
    /// e SnapBroken() no "On Restored Unlocked" (sala recarregada já resolvida).
    /// Sequência: a manivela gira, as correntes somem, o lustre despenca (gravidade) até o chão,
    /// o lustre inteiro some e os destroços (com o Livro Antigo) aparecem, com som de vidro quebrando.
    /// </summary>
    public class LustreQueda : MonoBehaviour
    {
        [Tooltip("Lustre inteiro (cai).")]
        public Transform intact;
        [Tooltip("Destroços no chão (inativo até cair; inclui o Livro).")]
        public GameObject broken;
        [Tooltip("Correntes que prendem o lustre (somem ao soltar).")]
        public GameObject[] chains = new GameObject[0];
        [Tooltip("Manivela encaixada no mecanismo (gira antes de soltar).")]
        public Transform crank;
        [Tooltip("Altura local do chão onde o lustre bate (no pivô do lustre).")]
        public float floorY = 0f;
        public float crankSeconds = 1.2f;
        public AudioSource crashSound;
        public AudioSource crankSound;

        bool done;

        public void Fall()
        {
            if (done) return;
            done = true;
            StartCoroutine(Sequence());
        }

        public void SnapBroken()
        {
            done = true;
            foreach (var c in chains) if (c != null) c.SetActive(false);
            if (intact != null) intact.gameObject.SetActive(false);
            if (broken != null) broken.SetActive(true);
        }

        IEnumerator Sequence()
        {
            if (crankSound != null) crankSound.Play();
            for (float t = 0f; t < crankSeconds; t += Time.deltaTime)
            {
                if (crank != null) crank.Rotate(0f, 0f, -540f * Time.deltaTime, Space.Self);
                yield return null;
            }

            foreach (var c in chains) if (c != null) c.SetActive(false);

            // Queda livre com uma leve rotação (não é perfeitamente reto).
            if (intact != null)
            {
                float v = 0f;
                var spin = new Vector3(Random.Range(-25f, 25f), Random.Range(-40f, 40f), Random.Range(-25f, 25f));
                while (intact.localPosition.y > floorY)
                {
                    v += 9.81f * Time.deltaTime;
                    var p = intact.localPosition;
                    p.y = Mathf.Max(floorY, p.y - v * Time.deltaTime);
                    intact.localPosition = p;
                    intact.Rotate(spin * Time.deltaTime, Space.Self);
                    yield return null;
                }
            }

            if (crashSound != null) crashSound.Play();
            if (intact != null) intact.gameObject.SetActive(false);
            if (broken != null) broken.SetActive(true);
        }
    }
}
