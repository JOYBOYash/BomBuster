using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    public Transform cameraTransform;
    public Transform weaponHolder;
    public FPSPlayerController player;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.5f;
    public float rotationSmoothTime = 0.05f;
    public float maxLookAngle = 80f;

    [Header("Weapon Follow")]
    public float weaponFollowSpeed = 15f;

    [Header("Head Bob")]
    public float bobSpeed = 12f;
    public float bobAmount = 0.05f;
    public float sprintBobMultiplier = 1.4f;

    float xRotation;
    Vector2 currentMouseDelta;
    Vector2 mouseDeltaVelocity;

    Vector3 cameraDefaultPos;
    float bobTimer;

    InputAction lookAction;

    void Awake()
    {
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        cameraDefaultPos = transform.localPosition;
    }

    void OnEnable()
    {
        lookAction.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        lookAction.Disable();
    }

    void Update()
    {
        HandleMouseLook();
        HandleHeadBob();
        HandleWeaponFollow();
    }

    // ================= CAMERA LOOK =================

    void HandleMouseLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta,
            lookInput,
            ref mouseDeltaVelocity,
            rotationSmoothTime
        );

        xRotation -= currentMouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * currentMouseDelta.x);
    }

    // ================= HEAD BOB =================

    void HandleHeadBob()
    {
        if (player.CurrentVelocity.magnitude > 0.1f && player.IsSprinting || player.CurrentVelocity.magnitude > 0.1f)
        {
            float speed = player.IsSprinting ? bobSpeed * sprintBobMultiplier : bobSpeed;
            float amount = player.IsSprinting ? bobAmount * sprintBobMultiplier : bobAmount;

            bobTimer += Time.deltaTime * speed;

            transform.localPosition = cameraDefaultPos +
                new Vector3(
                    Mathf.Sin(bobTimer) * amount,
                    Mathf.Cos(bobTimer * 2f) * amount,
                    0f
                );
        }
        else
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                cameraDefaultPos,
                Time.deltaTime * 8f
            );
        }
    }

    // ================= WEAPON FOLLOW =================

    void HandleWeaponFollow()
    {
        if (weaponHolder == null) return;

        Quaternion targetRot = Quaternion.Euler(xRotation, 0f, 0f);

        weaponHolder.localRotation = Quaternion.Slerp(
            weaponHolder.localRotation,
            targetRot,
            weaponFollowSpeed * Time.deltaTime
        );
    }

    // ================= EXTERNAL HOOKS =================

    public void AddCameraKick(float kick)
    {
        xRotation -= kick;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
    }
}
