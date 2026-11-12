using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Somnium.Core
{
    /// <summary>Estado global simples do jogo: menus abertos e VR ativo.</summary>
    public static class GameState
    {
        /// <summary>True enquanto um menu (inventário, teclado numérico...) está aberto. O jogador não anda nem interage.</summary>
        public static bool InMenu { get; set; }

        static int menuClosedFrame = -1;

        /// <summary>Chamar ao fechar um menu: evita que o mesmo botão (ex.: B = pular) vaze para o jogo.</summary>
        public static void MarkMenuClosed() => menuClosedFrame = Time.frameCount;

        public static bool MenuClosedThisFrame => Time.frameCount == menuClosedFrame;

        static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

        /// <summary>True quando o Cardboard/headset está renderizando em estéreo.</summary>
        public static bool XRActive
        {
            get
            {
                SubsystemManager.GetSubsystems(displays);
                foreach (var d in displays)
                    if (d.running) return true;
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            InMenu = false;
            menuClosedFrame = -1;
        }
    }

    /// <summary>
    /// Memória do mundo: o que já foi pego, destrancado ou examinado.
    /// As salas são cenas carregadas e descarregadas (ScenePartLoader), então os objetos consultam
    /// isto no Start para voltar ao estado certo. O save em disco vai ser construído em cima disto.
    /// </summary>
    public static class WorldState
    {
        static readonly HashSet<string> flags = new HashSet<string>();

        public static bool Has(string key) => !string.IsNullOrEmpty(key) && flags.Contains(key);
        public static void Set(string key) { if (!string.IsNullOrEmpty(key)) flags.Add(key); }
        public static void Clear() => flags.Clear();
        public static IEnumerable<string> All => flags;

        /// <summary>
        /// Chave estável para um objeto de cena: usa o id explícito, se houver; senão
        /// "prefixo:cena:caminho/na/hierarquia". Evite dois objetos com o mesmo caminho.
        /// </summary>
        public static string KeyFor(Component c, string prefix, string explicitId)
        {
            if (!string.IsNullOrEmpty(explicitId)) return prefix + ":" + explicitId;
            var t = c.transform;
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return prefix + ":" + c.gameObject.scene.name + ":" + path;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => flags.Clear();
    }
}
