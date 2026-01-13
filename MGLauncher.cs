using UnityEngine;
using UnityEngine.InputSystem;


        public enum RadiusOrientation
    {
        SurfaceAligned,
        AlwaysHorizontal,
        AlwaysVertical
    }

public class MGLLauncher : MonoBehaviour
{



  


    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public GameObject bombPrefab;
    public LineRenderer trajectoryLine;
    public LineRenderer explosionRadiusLine;

    [Header("Firing")]
    public float launchForce = 22f;
    public float fireRate = 0.6f;

    [Header("Trajectory")]
    public int trajectoryPoints = 30;
    public float trajectoryTimeStep = 0.1f;
    public LayerMask trajectoryCollisionLayers;

    [Header("Explosion Preview")]
    public float explosionRadius = 6f;
    public int explosionCircleSegments = 32;

          [System.Serializable]
    public struct RadiusLayerRule
    {
        public LayerMask layer;
        public RadiusOrientation orientation;
    }

    [Header("Explosion Radius Orientation")]
    public RadiusOrientation defaultRadiusOrientation = RadiusOrientation.SurfaceAligned;
    public RadiusLayerRule[] radiusLayerRules;


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
            DrawTrajectoryWithCollision();
        }
    }

    // ================= INPUT =================

    private void OnFireStarted(InputAction.CallbackContext ctx)
    {
        if (Time.time < nextFireTime) return;

        isAiming = true;
        trajectoryLine.enabled = true;
        explosionRadiusLine.enabled = true;
    }

    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        if (!isAiming) return;

        FireBomb();

        trajectoryLine.positionCount = 0;
        explosionRadiusLine.positionCount = 0;
        trajectoryLine.enabled = false;
        explosionRadiusLine.enabled = false;

        isAiming = false;
        nextFireTime = Time.time + fireRate;
    }

    // ================= FIRING =================

    void FireBomb()
    {
        Vector3 spawnPos = firePoint.position + playerCamera.transform.forward * 0.5f;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 direction = playerCamera.transform.forward;
        direction = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        ) * direction;

        rb.linearVelocity = direction * launchForce;
    }

    // ================= TRAJECTORY + COLLISION =================

    void DrawTrajectoryWithCollision()
    {
        trajectoryLine.positionCount = 0;
        explosionRadiusLine.positionCount = 0;

        Vector3 startPos = firePoint.position;
        Vector3 velocity = playerCamera.transform.forward * launchForce;
        Vector3 gravity = Physics.gravity;

        Vector3 previousPoint = startPos;
        trajectoryLine.positionCount = 1;
        trajectoryLine.SetPosition(0, previousPoint);

        for (int i = 1; i < trajectoryPoints; i++)
        {
            float t = i * trajectoryTimeStep;

            Vector3 nextPoint =
                startPos +
                velocity * t +
                0.5f * gravity * t * t;

            // Raycast between previous & next point
            if (Physics.Raycast(
                previousPoint,
                nextPoint - previousPoint,
                out RaycastHit hit,
                Vector3.Distance(previousPoint, nextPoint),
                trajectoryCollisionLayers
            ))
            {
                // Stop trajectory at hit
                trajectoryLine.positionCount++;
                trajectoryLine.SetPosition(trajectoryLine.positionCount - 1, hit.point);

               DrawExplosionRadius(hit.point, hit.normal, hit.collider);


                return;
            }

            trajectoryLine.positionCount++;
            trajectoryLine.SetPosition(trajectoryLine.positionCount - 1, nextPoint);

            previousPoint = nextPoint;
        }
    }

    // ================= EXPLOSION RADIUS =================

void DrawExplosionRadius(Vector3 center, Vector3 normal, Collider hitCollider)
{
    RadiusOrientation orientation = GetOrientationForLayer(hitCollider.gameObject.layer);

    Quaternion rotation;

    switch (orientation)
    {
        case RadiusOrientation.AlwaysHorizontal:
            rotation = Quaternion.identity; // flat on XZ plane
            break;

        case RadiusOrientation.AlwaysVertical:
            rotation = Quaternion.Euler(90f, 0f, 0f); // vertical circle
            break;

        default: // SurfaceAligned
            rotation = Quaternion.FromToRotation(Vector3.up, normal);
            break;
    }

    explosionRadiusLine.positionCount = explosionCircleSegments;

    for (int i = 0; i < explosionCircleSegments; i++)
    {
        float angle = (float)i / explosionCircleSegments * Mathf.PI * 2f;

        Vector3 localPos = new Vector3(
            Mathf.Cos(angle) * explosionRadius,
            0f,
            Mathf.Sin(angle) * explosionRadius
        );

        explosionRadiusLine.SetPosition(
            i,
            center + rotation * localPos
        );
    }
}

RadiusOrientation GetOrientationForLayer(int layer)
{
    foreach (var rule in radiusLayerRules)
    {
        if ((rule.layer.value & (1 << layer)) != 0)
            return rule.orientation;
    }

    return defaultRadiusOrientation;
}


}
