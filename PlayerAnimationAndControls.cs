using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FPSPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.6f;

    [Header("Jump Settings")]
    public float jumpForce = 6.5f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.2f;
    public float groundedStickForce = -2f;

    [Header("Audio")]
    public AudioClip walkLoop;
    public AudioClip sprintLoop;
    public AudioClip jumpClip;
    public AudioClip landClip;

    [Header("Obstacle Impact Audio")]
    public AudioClip obstacleHitClip;
    [Range(0f, 1f)] public float obstacleHitVolume = 0.8f;
    public float obstacleHitCooldown = 0.35f;

    [Header("Obstacle Detection")]
    [Tooltip("Layers considered solid obstacles (props, walls, ghosts, etc.)")]
    public LayerMask obstacleLayers;

    [Range(0f, 1f)] public float walkVolume = 0.6f;
    [Range(0f, 1f)] public float sprintVolume = 0.8f;
    [Range(0f, 1f)] public float jumpVolume = 0.9f;

    float lastObstacleHitTime;
    CollisionFlags lastCollisionFlags;
    bool hitValidObstacleThisFrame;

    public Vector3 CurrentVelocity { get; private set; }
    public bool IsSprinting { get; private set; }

    CharacterController controller;
    AudioSource audioSource;

    Vector2 moveInput;
    float yVelocity;
    bool wasGrounded;

    // ---------- INPUT ----------
    InputAction moveAction;
    InputAction sprintAction;
    InputAction jumpAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        hitValidObstacleThisFrame = false; // reset every frame

        ReadInput();
        HandleMovement();
        HandleAudio();
    }

    // ================= INPUT =================

    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        IsSprinting = sprintAction.IsPressed() && moveInput.y > 0.1f;
    }

    // ================= MOVEMENT =================

    void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        Vector3 move =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        float speed = IsSprinting
            ? moveSpeed * sprintMultiplier
            : moveSpeed;

        if (grounded)
        {
            if (!wasGrounded && landClip != null)
                AudioSource.PlayClipAtPoint(landClip, transform.position, jumpVolume);

            yVelocity = groundedStickForce;

            if (jumpAction.WasPressedThisFrame())
            {
                yVelocity = jumpForce;
                PlayOneShot(jumpClip, jumpVolume);
            }
        }
        else
        {
            float gravityMultiplier = yVelocity < 0 ? fallMultiplier : 1f;
            yVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }

        Vector3 velocity = move * speed + Vector3.up * yVelocity;

        lastCollisionFlags = controller.Move(velocity * Time.deltaTime);

        CurrentVelocity = new Vector3(velocity.x, 0f, velocity.z);

        HandleObstacleImpact(move, speed);

        wasGrounded = grounded;
    }

    // ================= OBSTACLE COLLISION =================

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if ((obstacleLayers.value & (1 << hit.gameObject.layer)) != 0)
        {
            hitValidObstacleThisFrame = true;
        }
    }

    void HandleObstacleImpact(Vector3 moveDir, float speed)
    {
        if (!controller.isGrounded)
            return;

        if (moveDir.magnitude < 0.1f)
            return;

        bool hitSide = (lastCollisionFlags & CollisionFlags.Sides) != 0;
        bool movementBlocked = CurrentVelocity.magnitude < 0.05f;

        if (hitSide && movementBlocked && hitValidObstacleThisFrame)
        {
            StopLoop();

            if (Time.time - lastObstacleHitTime >= obstacleHitCooldown)
            {
                PlayOneShot(obstacleHitClip, obstacleHitVolume);
                lastObstacleHitTime = Time.time;
            }
        }
    }

    // ================= AUDIO =================

    void HandleAudio()
    {
        bool isMoving = moveInput.magnitude > 0.1f && controller.isGrounded;

        if (!isMoving)
        {
            StopLoop();
            return;
        }

        if (IsSprinting)
            PlayLoop(sprintLoop, sprintVolume);
        else
            PlayLoop(walkLoop, walkVolume);
    }

    void PlayLoop(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    void StopLoop()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}
