using UnityEngine;
using UnityEngine.InputSystem;

public class MGLLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public GameObject bombPrefab;

    [Header("Camera Shake")]
    public float minShakeIntensity = 0.15f;
    public float maxShakeIntensity = 0.45f;
    public float shakeDuration = 0.35f;


    [Header("Firing")]
    public float baseLaunchForce = 22f;
    public float fireRate = 0.6f;
    public float spreadAngle = 1.5f;

    [Header("Dependencies")]
    public MGLOscillatingAim aimSystem;

    private float nextFireTime;
    private InputAction fireAction;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        fireAction.Enable();
        fireAction.canceled += OnFireReleased;
    }

    void OnDisable()
    {
        fireAction.canceled -= OnFireReleased;
        fireAction.Disable();
    }

    void OnFireReleased(InputAction.CallbackContext ctx)
    {
        if (Time.time < nextFireTime || !aimSystem.IsAiming)
            return;

        FireBomb();
        nextFireTime = Time.time + fireRate;
    }

    void FireBomb()
    {
        Vector3 spawnPos = firePoint.position + playerCamera.transform.forward * 0.5f;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 dir = playerCamera.transform.forward;
        dir = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        ) * dir;

        float finalForce = baseLaunchForce * aimSystem.CurrentLaunchMultiplier;
        rb.linearVelocity = dir * finalForce;

        if (bomb.TryGetComponent(out BombProjectile bombLogic))
        {
            bombLogic.SetChargeValues(
                aimSystem.CurrentExplosionRadius,
                aimSystem.CurrentDamageMultiplier
            );
        }

        float inverted = 1f - aimSystem.CurrentCharge01;

        // stronger shake at perfect timing
        float shakeIntensity = Mathf.Lerp(
            minShakeIntensity,
            maxShakeIntensity,
            inverted
        );

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(
                shakeIntensity,
                shakeDuration
            );
        }
        aimSystem.StopAiming();
    }
}
