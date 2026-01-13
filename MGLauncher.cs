using UnityEngine;
using UnityEngine.InputSystem;

public class MGLLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public GameObject bombPrefab;
    public LineRenderer trajectoryLine;

    [Header("Firing")]
    public float launchForce = 22f;
    public float fireRate = 0.6f;

    [Header("Trajectory")]
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.1f;

    [Header("Spread")]
    public float spreadAngle = 1.5f;

    private float nextFireTime;
    private bool isAiming;

    private InputAction fireAction;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        fireAction.Enable();
        fireAction.started += OnFireStarted;
        fireAction.canceled += OnFireCanceled;
    }

    void OnDisable()
    {
        fireAction.started -= OnFireStarted;
        fireAction.canceled -= OnFireCanceled;
        fireAction.Disable();
    }

    void Update()
    {
        if (isAiming)
        {
            DrawTrajectory();
        }
    }

    // ================= INPUT =================

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        // Hold LMB → show trajectory
        if (Time.time < nextFireTime) return;

        isAiming = true;
        trajectoryLine.enabled = true;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        // Release LMB → shoot
        if (!isAiming) return;

        FireBomb();
        trajectoryLine.enabled = false;
        trajectoryLine.positionCount = 0;
        isAiming = false;

        nextFireTime = Time.time + fireRate;
    }

    // ================= FIRING =================

    void FireBomb()
    {
        // 1️⃣ Spawn slightly forward so it doesn't intersect player
        Vector3 spawnPos = firePoint.position + playerCamera.transform.forward * 0.5f;

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        Collider bombCollider = bomb.GetComponent<Collider>();

        // 2️⃣ Ignore collision with player
        Collider playerCollider = playerCamera.GetComponentInParent<Collider>();
        if (playerCollider != null && bombCollider != null)
        {
            Physics.IgnoreCollision(bombCollider, playerCollider);
        }

        // 3️⃣ Calculate firing direction
        Vector3 direction = playerCamera.transform.forward;

        direction = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        ) * direction;

        // 4️⃣ Correct Rigidbody launch
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = direction * launchForce;
    }


    // ================= TRAJECTORY =================

    void DrawTrajectory()
    {
        if (trajectoryLine == null) return;

        trajectoryLine.positionCount = trajectoryPoints;

        Vector3 startPos = firePoint.position;
        Vector3 startVelocity = playerCamera.transform.forward * launchForce;
        Vector3 gravity = Physics.gravity;

        for (int i = 0; i < trajectoryPoints; i++)
        {
            float t = i * trajectoryTimeStep;

            Vector3 point =
                startPos +
                startVelocity * t +
                0.5f * gravity * t * t;

            trajectoryLine.SetPosition(i, point);
        }
    }
}
