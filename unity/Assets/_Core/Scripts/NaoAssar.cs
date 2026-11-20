using UnityEngine;

namespace Somnium.Core
{
    /// <summary>
    /// Marca um objeto (e os filhos) como dinâmico para o bake de luz: não vira estático nem grava sombra no lightmap.
    /// Use em coisas que se movem, caem, aparecem ou somem (ex.: o lustre que despenca no Salão Invertido).
    /// Ele é iluminado pelas Light Probes.
    /// </summary>
    public class NaoAssar : MonoBehaviour { }
}
