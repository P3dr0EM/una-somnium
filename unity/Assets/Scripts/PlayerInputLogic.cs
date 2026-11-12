using Assets.Scripts;
using System;
using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class MouseLook : PortalTraveller
{
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    private PlayerInput playerInput;
    public Camera playerCamera;
    InputSystem_Actions inputSystem_Actions;
    public CharacterController playerController;
    public float speed = 12f;
    public float gravity = -19.62f;
    public float jumpHeight = 3f;

    public Transform groundCheck;
    public float groundDistance = .4f;
    public LayerMask groundLayerMask;
    Vector3 velocity;
    Boolean isGrounded;


    private float interactableRange = 6.0f;
    [SerializeField] private LayerMask interactableLayerMask;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        inputSystem_Actions = new InputSystem_Actions();

        inputSystem_Actions.Player.Enable();
    }

    private void Handle_Movement()
    {
        Vector2 movementInput = inputSystem_Actions.Player.Move.ReadValue<Vector2>();
        Vector3 moveVector = transform.right * movementInput.x + transform.forward * movementInput.y;
        playerController.Move(moveVector * speed * Time.deltaTime);

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayerMask);

        if (inputSystem_Actions.Player.Jump.WasPerformedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        if (isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);

    }
    float xRotation = 0f;
    private void Handle_Rotation()
    {
        float mouseX = inputSystem_Actions.Player.Look.ReadValue<Vector2>().x * mouseSensitivity * Time.deltaTime;
        float mouseY = inputSystem_Actions.Player.Look.ReadValue<Vector2>().y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        inputSystem_Actions.Player.Attack.performed += OnInputAttackPerformed;
    }

    private void OnDestroy()
    {
        inputSystem_Actions.Player.Attack.performed -= OnInputAttackPerformed;
    }

    void OnInputAttackPerformed(InputAction.CallbackContext context)
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactableRange, interactableLayerMask))
        {
            if (hit.collider.TryGetComponent(out Interactable interactable)) interactable.Interact();
        }
    }
    void Update()
    {
        Handle_Rotation();
        Handle_Movement();
    }
}
