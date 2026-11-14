using System.Collections;
using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using UnityEngine;

namespace Somnium.Rooms
{
    /// <summary>
    /// Gato da Colinas XP. Sempre sentado e virado para o jogador.
    /// Quando o jogador chega perto, ele "pisca" (encolhe e some) e reaparece longe, mas:
    ///  - a no máximo <see cref="maxDistanceFromCenter"/> m do centro da sala (a porta);
    ///  - a pelo menos <see cref="minDistanceFromPlayer"/> m do jogador;
    ///  - dentro do campo de visão do jogador (para não "sumir" nas costas dele, o que em VR parece bug).
    /// Com o item que o acalma (Lata de Ração) no inventário, ou depois de alimentado, ele fica parado.
    /// A entrega dos itens fica num ItemLock no próprio gato (requer a Lata; dá Chave 1 + Metade do Medalhão).
    /// </summary>
    public class GatoTeleporte : MonoBehaviour
    {
        [Header("Área")]
        [Tooltip("Centro da área do gato (a porta da sala).")]
        public Transform center;
        public float maxDistanceFromCenter = 30f;
        public float minDistanceFromPlayer = 10f;
        public float maxDistanceFromPlayer = 24f;

        [Header("Fuga")]
        [Tooltip("Distância em que o gato foge.")]
        public float triggerDistance = 3f;
        [Tooltip("Meio ângulo, em graus, do cone à frente do jogador onde o gato pode reaparecer.")]
        public float viewHalfAngle = 45f;
        public float blinkSeconds = 0.18f;
        [Tooltip("Chão onde o gato pode aparecer (layer Ground).")]
        public LayerMask groundMask = ~0;

        [Header("Alimentar")]
        [Tooltip("Com este item no inventário o gato não foge.")]
        public ItemData calmItem;
        [Tooltip("ItemLock que entrega os itens (no próprio gato).")]
        public ItemLock feedLock;
        [Tooltip("Pote de ração que aparece ao lado do gato quando ele é alimentado.")]
        public Transform bowl;

        [Header("Som (opcional)")]
        public AudioSource meow;

        Transform player;
        Vector3 baseScale;
        bool blinking;

        bool IsCalm =>
            (feedLock != null && feedLock.IsUnlocked) ||
            (calmItem != null && Inventory.Instance.Has(calmItem));

        void Awake() => baseScale = transform.localScale;

        void Start()
        {
            if (feedLock != null)
            {
                feedLock.onUnlocked.AddListener(PlaceBowl);
                feedLock.onRestoredUnlocked.AddListener(PlaceBowl);
                if (feedLock.IsUnlocked) PlaceBowl();
            }
        }

        void Update()
        {
            if (player == null)
            {
                var cam = Camera.main;
                if (cam == null) return;
                player = cam.transform;
            }

            FacePlayer(Time.deltaTime * 4f);

            if (blinking || IsCalm || GameState.InMenu || RoomTransition.InProgress) return;
            if (FlatDistance(transform.position, player.position) < triggerDistance)
                StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            blinking = true;
            if (meow != null) meow.Play();

            yield return Scale(1f, 0f);
            transform.position = PickDestination();
            FacePlayer(1f);
            yield return Scale(0f, 1f);

            blinking = false;
        }

        IEnumerator Scale(float from, float to)
        {
            for (float t = 0f; t < blinkSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(from, to, t / blinkSeconds);
                transform.localScale = baseScale * k;
                yield return null;
            }
            transform.localScale = baseScale * to;
        }

        /// <summary>Ponto no chão à frente do jogador, longe dele e dentro da área da sala.</summary>
        Vector3 PickDestination()
        {
            Vector3 c = center != null ? center.position : transform.position;
            Vector3 p = player.position;
            Vector3 fwd = Vector3.ProjectOnPlane(player.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.ProjectOnPlane(player.up, Vector3.up); // olhando para cima/baixo
            fwd.Normalize();

            for (int attempt = 0; attempt < 60; attempt++)
            {
                // Depois de 30 tentativas, abre o cone (jogador olhando para fora da área).
                float half = attempt < 30 ? viewHalfAngle : 120f;
                Vector3 dir = Quaternion.Euler(0f, Random.Range(-half, half), 0f) * fwd;
                Vector3 candidate = p + dir * Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
                if (FlatDistance(candidate, c) > maxDistanceFromCenter) continue;
                if (TryGround(candidate, out var ground)) return ground;
            }

            // Último recurso: do outro lado da porta em relação ao jogador.
            Vector3 away = Vector3.ProjectOnPlane(c - p, Vector3.up).normalized;
            if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
            Vector3 fallback = c + away * Mathf.Min(15f, maxDistanceFromCenter);
            return TryGround(fallback, out var g) ? g : fallback;
        }

        bool TryGround(Vector3 xz, out Vector3 point)
        {
            var origin = new Vector3(xz.x, xz.y + 60f, xz.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }
            point = xz;
            return false;
        }

        void FacePlayer(float t)
        {
            if (player == null) return;
            Vector3 to = Vector3.ProjectOnPlane(player.position - transform.position, Vector3.up);
            if (to.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), Mathf.Clamp01(t));
        }

        void PlaceBowl()
        {
            if (bowl == null) return;
            bowl.gameObject.SetActive(true);
            Vector3 front = transform.position + transform.forward * 0.45f;
            bowl.position = TryGround(front, out var g) ? g : front;
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void OnDrawGizmosSelected()
        {
            if (center == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
            const int n = 48;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Gizmos.DrawLine(center.position + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * maxDistanceFromCenter,
                                center.position + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * maxDistanceFromCenter);
            }
        }
    }
}
