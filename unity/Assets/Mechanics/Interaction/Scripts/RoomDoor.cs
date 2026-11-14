using Assets.Scripts;
using Somnium.Core;
using UnityEngine;

namespace Somnium.Interaction
{
    /// <summary>
    /// Porta que leva a outra sala (outra cena). Interagir: fade, carrega a sala de destino,
    /// põe o jogador no PlayerSpawnPoint "Spawn Id" de lá e descarrega a sala atual.
    /// A cena de destino precisa estar em File > Build Profiles > Scene List.
    /// </summary>
    public class RoomDoor : Interactable
    {
        [Tooltip("Caminho da cena de destino, ex.: Assets/Rooms/01_ColinasXP/colinas_xp.unity")]
        public string targetScenePath;

        [Tooltip("Id do PlayerSpawnPoint na sala de destino.")]
        public string spawnId = "inicio";

        public override string Prompt => string.IsNullOrEmpty(promptText) ? "Entrar" : promptText;

        public override bool CanInteract => base.CanInteract && !RoomTransition.InProgress;

        public override void Interact() => RoomTransition.Go(targetScenePath, spawnId, gameObject.scene);
    }
}
