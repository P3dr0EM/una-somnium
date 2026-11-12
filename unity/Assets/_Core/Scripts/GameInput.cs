using UnityEngine;
using UnityEngine.InputSystem;

namespace Somnium.Core
{
    /// <summary>
    /// Ponto único de leitura de input. Usa as ações "project-wide" (InputSystem.actions),
    /// definidas em Assets/InputSystem_Actions.inputactions.
    /// Também detecta se o jogador está no gamepad ou no teclado/mouse, para os textos de dica.
    /// </summary>
    public static class GameInput
    {
        static InputAction move, look, jump, sprint, interact, inventory, combine;
        static bool initialized;

        /// <summary>True se o último input veio de um gamepad.</summary>
        public static bool UsingGamepad { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            if (initialized) InputSystem.onActionChange -= OnActionChange;
            initialized = false;
            UsingGamepad = false;
            move = look = jump = sprint = interact = inventory = combine = null;
        }

        static void Init()
        {
            if (initialized) return;
            initialized = true;
            move = Find("Player/Move");
            look = Find("Player/Look");
            jump = Find("Player/Jump");
            sprint = Find("Player/Sprint");
            interact = Find("Player/Interact");
            inventory = Find("Player/Inventory");
            combine = Find("Player/Combine");
            InputSystem.onActionChange += OnActionChange;
        }

        static InputAction Find(string path)
        {
            var asset = InputSystem.actions;
            var action = asset != null ? asset.FindAction(path) : null;
            if (action == null)
                Debug.LogWarning($"[GameInput] Ação '{path}' não encontrada. Confira o InputSystem_Actions e se ele está " +
                                 "como Project-wide Actions (Edit > Project Settings > Input System Package).");
            return action;
        }

        static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is InputAction a && a.activeControl != null)
                UsingGamepad = a.activeControl.device is Gamepad;
        }

        /// <summary>O componente PlayerInput pode desligar mapas; garantimos que a ação esteja ligada.</summary>
        static InputAction Ready(InputAction a)
        {
            Init();
            if (a != null && !a.enabled) a.Enable();
            return a;
        }

        static bool Pressed(InputAction a)
        {
            a = Ready(a);
            return a != null && a.WasPressedThisFrame();
        }

        static Vector2 ReadVector(InputAction a)
        {
            a = Ready(a);
            return a != null ? a.ReadValue<Vector2>() : Vector2.zero;
        }

        // ---------- Gameplay ----------
        public static Vector2 Move { get { Init(); return ReadVector(move); } }
        public static Vector2 LookRaw { get { Init(); return ReadVector(look); } }

        /// <summary>True se o Look atual vem do mouse (delta em pixels), e não de um analógico.</summary>
        public static bool LookFromPointer
        {
            get
            {
                Init();
                var a = Ready(look);
                return a != null && a.activeControl != null && a.activeControl.device is Pointer;
            }
        }

        public static bool JumpPressed { get { Init(); return Pressed(jump); } }
        public static bool SprintHeld { get { Init(); var a = Ready(sprint); return a != null && a.IsPressed(); } }
        public static bool InteractPressed { get { Init(); return Pressed(interact); } }
        public static bool InventoryPressed { get { Init(); return Pressed(inventory); } }
        public static bool CombinePressed { get { Init(); return Pressed(combine); } }

        /// <summary>Voltar/cancelar em menus: Esc ou B (East) no gamepad.</summary>
        public static bool BackPressed
        {
            get
            {
                var kb = Keyboard.current;
                var gp = Gamepad.current;
                return (kb != null && kb.escapeKey.wasPressedThisFrame) ||
                       (gp != null && gp.buttonEast.wasPressedThisFrame);
            }
        }

        /// <summary>Navegação de menu: analógico esquerdo, WASD, setas e D-pad.</summary>
        public static Vector2 Navigate
        {
            get
            {
                Vector2 v = Move;
                var gp = Gamepad.current;
                if (gp != null) v += gp.dpad.ReadValue();
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>
        /// Rotação em graus para este frame. Mouse: graus por pixel. Analógico: graus por segundo
        /// (usa tempo não escalado, então funciona com o jogo pausado).
        /// </summary>
        public static Vector2 LookDegrees(float mouseDegreesPerPixel, float stickDegreesPerSecond)
        {
            Vector2 raw = LookRaw;
            return LookFromPointer
                ? raw * mouseDegreesPerPixel
                : raw * stickDegreesPerSecond * Time.unscaledDeltaTime;
        }

        // ---------- Rótulos dos botões para as dicas na tela ----------
        public static string InteractLabel => UsingGamepad ? "A" : "E";
        public static string InventoryLabel => UsingGamepad ? "Y" : "Tab";
        public static string CombineLabel => UsingGamepad ? "X" : "Q";
        public static string BackLabel => UsingGamepad ? "B" : "Esc";
        public static string NavigateLabel => UsingGamepad ? "Analógico E" : "WASD";
        public static string RotateLabel => UsingGamepad ? "Analógico D" : "Mouse";
    }
}
