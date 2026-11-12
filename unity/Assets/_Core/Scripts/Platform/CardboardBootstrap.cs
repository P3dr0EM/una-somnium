using Google.XR.Cardboard;
using UnityEngine;

namespace Somnium.Core
{
    /// <summary>
    /// Substitui o CardboardStartup do sample "Hello Cardboard".
    /// Faz o mesmo (tela sempre acesa, leitura do QR code do óculos, botão de engrenagem/fechar, recentrar),
    /// mas só quando o Cardboard está ativo. No PC/editor o loader do Cardboard não existe, e chamar
    /// a API gerava "Please initialize Cardboard XR loader before calling this function" a cada frame.
    /// </summary>
    public class CardboardBootstrap : MonoBehaviour
    {
        bool initialized;

        void Update()
        {
            if (!GameState.XRActive) return;

            if (!initialized)
            {
                initialized = true;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Screen.brightness = 1f;
                if (!Api.HasDeviceParams()) Api.ScanDeviceParams();
            }

            if (Api.IsGearButtonPressed) Api.ScanDeviceParams();
            if (Api.IsCloseButtonPressed) Application.Quit();
            if (Api.IsTriggerHeldPressed) Api.Recenter();
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();

            Api.UpdateScreenParams();
        }
    }
}
