using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSPlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.6f;   // 🏃 Sprint speed boost
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.5f;
    public float rotationSmoothTime = 0.05f;
    public float maxLookAngle = 80f;

    [Header("Weapon")]
    public Transform weaponHolder;        // Parent of the gun
    public float weaponFollowSpeed = 15f; // Smoothness


    [Header("Head Bob")]
    public float bobSpeed = 12f;
    public float bobAmount = 0.05f;
    public float sprintBobMultiplier = 1.4f; // 💥 Stronger bob when sprinting

    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yVelocity;

    private float xRotation;
    private Vector2 currentMouseDelta;
    private Vector2 mouseDeltaVelocity;

    private Vector3 cameraDefaultPos;
    private float bobTimer;

    private bool isSprinting;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cameraDefaultPos = cameraHolder.localPosition;

        // Movement (WASD)
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // Mouse Look
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");

        // Sprint (Shift)
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        sprintAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

        private void HandleWeaponFollow()
    {
        if (weaponHolder == null)
            return;

        // Match camera pitch (X rotation)
        Quaternion targetRotation = Quaternion.Euler(
            xRotation,
            0f,
            0f
        );

        weaponHolder.localRotation = Quaternion.Slerp(
            weaponHolder.localRotation,
            targetRotation,
            weaponFollowSpeed * Time.deltaTime
        );
    }


    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        sprintAction.Disable();
    }

    void Update()
    {
        ReadInput();
        HandleMouseLook();
        HandleMovement();
        HandleHeadBob();
        HandleWeaponFollow(); // 🔥 THIS IS THE FIX
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;
        isSprinting = sprintAction.IsPressed() && moveInput.y > 0.1f; 
        // 👆 sprint only when moving forward (FPS standard)
    }

    private void HandleMouseLook()
    {
        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta,
            lookInput,
            ref mouseDeltaVelocity,
            rotationSmoothTime
        );

        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * currentMouseDelta.x);
    }

    private void HandleMovement()
    {
        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        float currentSpeed = isSprinting
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        if (controller.isGrounded)
            yVelocity = -2f;
        else
            yVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * currentSpeed + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        if (controller.isGrounded && moveInput.magnitude > 0.1f)
        {
            float speed = isSprinting ? bobSpeed * sprintBobMultiplier : bobSpeed;
            float amount = isSprinting ? bobAmount * sprintBobMultiplier : bobAmount;

            bobTimer += Time.deltaTime * speed;

            cameraHolder.localPosition = cameraDefaultPos +
                new Vector3(
                    Mathf.Sin(bobTimer) * amount,
                    Mathf.Cos(bobTimer * 2f) * amount,
                    0f
                );
        }
        else
        {
            bobTimer = 0f;
            cameraHolder.localPosition = Vector3.Lerp(
                cameraHolder.localPosition,
                cameraDefaultPos,
                Time.deltaTime * 8f
            );
        }
    }
}
