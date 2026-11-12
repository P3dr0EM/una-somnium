using Somnium.Core;
using Somnium.Interaction;
using Somnium.Items;
using UnityEngine;

/// <summary>
/// Controle do jogador para PC (mouse + teclado ou gamepad) e Cardboard (cabeça + gamepad bluetooth).
///  - PC: mouse/analógico direito giram a câmera.
///  - VR: a cabeça controla o olhar (TrackedPoseDriver na câmera); o analógico direito gira o corpo em passos.
///  - Andar é relativo para onde se olha. Pulo: Espaço / B. Correr: Shift / clique do analógico esquerdo.
/// Adiciona automaticamente a mira (PlayerInteractor) e o inventário (InventoryUI).
/// Herda de PortalTraveller para atravessar os portais do Caio (Assets/Mechanics/Portal).
/// </summary>
public class PlayerInputLogic : PortalTraveller
{
    [Header("Referências")]
    public Transform playerBody;
    public Camera playerCamera;
    public CharacterController playerController;
    public Transform groundCheck;
    public float groundDistance = .4f;
    public LayerMask groundLayerMask;

    [Header("Movimento")]
    [Tooltip("Metros por segundo. Em VR, velocidades baixas enjoam menos.")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 5f;
    public float gravity = -19.62f;
    [Tooltip("Altura do pulo, em metros.")]
    public float jumpHeightMeters = 1.1f;

    [Header("Câmera no PC")]
    [Tooltip("Graus por pixel de movimento do mouse.")]
    public float mouseDegreesPerPixel = 0.1f;
    [Tooltip("Graus por segundo com o analógico no máximo.")]
    public float stickDegreesPerSecond = 150f;

    [Header("Câmera no VR")]
    [Tooltip("No VR, o analógico direito gira o corpo em passos fixos (reduz enjoo).")]
    public bool vrSnapTurn = true;
    public float snapTurnDegrees = 30f;

    Vector3 velocity;
    bool isGrounded;
    float xRotation;
    bool snapReady = true;

    void Awake()
    {
        if (playerBody == null) playerBody = transform;
        if (playerController == null) playerController = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        if (!TryGetComponent<PlayerInteractor>(out var interactor)) interactor = gameObject.AddComponent<PlayerInteractor>();
        interactor.Setup(playerCamera);

        if (!TryGetComponent<InventoryUI>(out var inventoryUI)) inventoryUI = gameObject.AddComponent<InventoryUI>();
        inventoryUI.Setup(playerCamera);
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        bool blocked = GameState.InMenu || GameState.MenuClosedThisFrame;
        if (!blocked) HandleRotation();
        HandleMovement(blocked);
    }

    void HandleRotation()
    {
        Vector2 look = GameInput.LookDegrees(mouseDegreesPerPixel, stickDegreesPerSecond);

        if (GameState.XRActive)
        {
            // A inclinação vem da cabeça; aqui só giramos o corpo.
            if (vrSnapTurn && !GameInput.LookFromPointer)
            {
                float x = GameInput.LookRaw.x;
                if (snapReady && Mathf.Abs(x) > 0.7f)
                {
                    playerBody.Rotate(0f, Mathf.Sign(x) * snapTurnDegrees, 0f);
                    snapReady = false;
                }
                else if (Mathf.Abs(x) < 0.3f)
                {
                    snapReady = true;
                }
            }
            else
            {
                playerBody.Rotate(0f, look.x, 0f);
            }
            return;
        }

        xRotation = Mathf.Clamp(xRotation - look.y, -89f, 89f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(0f, look.x, 0f);
    }

    void HandleMovement(bool blocked)
    {
        isGrounded = groundCheck != null
            ? Physics.CheckSphere(groundCheck.position, groundDistance, groundLayerMask, QueryTriggerInteraction.Ignore)
            : playerController.isGrounded;
        if (isGrounded && velocity.y < 0f) velocity.y = -2f;

        if (!blocked)
        {
            Vector2 input = GameInput.Move;

            // Anda para onde a câmera olha (no VR, para onde a cabeça aponta), sempre no plano horizontal.
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = playerBody.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float speed = GameInput.SprintHeld ? sprintSpeed : walkSpeed;
            playerController.Move((right * input.x + forward * input.y) * speed * Time.deltaTime);

            if (GameInput.JumpPressed && isGrounded)
                velocity.y = Mathf.Sqrt(jumpHeightMeters * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Chamado pelo Portal ao atravessar. O CharacterController guarda a própria posição e
    /// desfaz mudanças diretas no transform, então ele é desligado durante o teleporte.
    /// Só o yaw (giro horizontal) é aplicado ao corpo; a inclinação da câmera é mantida.
    /// </summary>
    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        // Mantém a velocidade (queda/pulo) coerente com a orientação do portal de saída.
        velocity = toPortal.TransformVector(fromPortal.InverseTransformVector(velocity));
        PlaceAt(pos, rot.eulerAngles.y);
    }

    /// <summary>Move o jogador para uma posição, virado para o ângulo horizontal informado (graus).</summary>
    public void PlaceAt(Vector3 position, float yawDegrees)
    {
        bool controllerWasEnabled = playerController != null && playerController.enabled;
        if (controllerWasEnabled) playerController.enabled = false;

        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDegrees, 0f));

        if (controllerWasEnabled) playerController.enabled = true;
        Physics.SyncTransforms();
    }
}
