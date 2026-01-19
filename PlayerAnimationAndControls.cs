using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.6f;
    public float gravity = -9.81f;

    public Vector3 CurrentVelocity { get; private set; }
    public bool IsSprinting { get; private set; }

    CharacterController controller;

    Vector2 moveInput;
    float yVelocity;

    InputAction moveAction;
    InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
    }

    void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
    }

    void Update()
    {
        ReadInput();
        HandleMovement();
    }

    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        IsSprinting = sprintAction.IsPressed() && moveInput.y > 0.1f;
    }

    void HandleMovement()
    {
        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        float speed = IsSprinting
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        if (controller.isGrounded)
            yVelocity = -2f;
        else
            yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);

        CurrentVelocity = new Vector3(velocity.x, 0f, velocity.z);
    }
}
