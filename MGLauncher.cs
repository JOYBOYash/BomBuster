using UnityEngine;
using CartoonFX; // Cartoon FX Particle Text

public class MGLLauncher : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public GameObject bombPrefab;

    [Header("Player")]
    public FPSPlayerController playerController;

    [Header("Firing")]
    public float baseLaunchForce = 22f;
    public float fireRate = 0.6f;
    public float spreadAngle = 1.5f;

    [Header("Momentum Transfer")]
    [Tooltip("How much player movement contributes to shot force")]
    public float movementInfluence = 0.65f;

    [Tooltip("Max additional force from movement")]
    public float maxMovementBoost = 12f;

    [Header("Camera Shake")]
    public float minShakeIntensity = 0.15f;
    public float maxShakeIntensity = 0.45f;
    public float shakeDuration = 0.35f;

    [Header("Shot Feedback VFX (SINGLE)")]
    [Tooltip("Anchor point for shot feedback (camera / chest / weapon)")]
    public Transform playerVFXAnchor;

    [Tooltip("CartoonFX Particle Text prefab (Dynamic enabled)")]
    public GameObject shotFeedbackVFX;

    public float feedbackVfxLifetime = 1.5f;

    [Header("Dependencies")]
    public MGLPrecisionAim aimSystem;

    float nextFireTime;

    // ================= EVENT HOOKS =================

    void OnEnable()
    {
        if (aimSystem != null)
            aimSystem.OnFireRequested += HandleFireRequest;
    }

    void OnDisable()
    {
        if (aimSystem != null)
            aimSystem.OnFireRequested -= HandleFireRequest;
    }

    // ================= FIRING =================

    void HandleFireRequest()
    {
        if (aimSystem == null || Time.time < nextFireTime)
            return;

        FireBomb();
        SpawnShotFeedback();

        nextFireTime = Time.time + fireRate;
    }

    void FireBomb()
    {
        // -------- SPAWN --------
        Vector3 spawnPos =
            firePoint.position +
            playerCamera.transform.forward * 0.5f;

        GameObject bomb =
            Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // -------- DIRECTION --------
        Vector3 fireDir = playerCamera.transform.forward;
        fireDir = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        ) * fireDir;

        // -------- PRECISION FORCE --------
        float precisionForce =
            baseLaunchForce *
            aimSystem.CurrentLaunchMultiplier;

        // -------- PLAYER MOMENTUM --------
        Vector3 playerVelocity =
            playerController != null
                ? playerController.CurrentVelocity
                : Vector3.zero;

        float forwardSpeed =
            Vector3.Dot(playerVelocity, fireDir.normalized);

        float movementBoost =
            Mathf.Clamp(
                forwardSpeed * movementInfluence,
                0f,
                maxMovementBoost
            );

        rb.linearVelocity =
            fireDir * (precisionForce + movementBoost);

        // -------- PASS DAMAGE DATA --------
        if (bomb.TryGetComponent(out BombProjectile bombLogic))
        {
            bombLogic.SetChargeValues(
                aimSystem.CurrentExplosionRadius,
                aimSystem.CurrentDamageMultiplier
            );
        }

        // -------- CAMERA SHAKE --------
        float shake =
            Mathf.Lerp(
                minShakeIntensity,
                maxShakeIntensity,
                aimSystem.CurrentPrecision01
            );

        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shake, shakeDuration);
    }

    // ================= SHOT FEEDBACK =================

    void SpawnShotFeedback()
    {
        if (shotFeedbackVFX == null || playerVFXAnchor == null)
            return;

        GameObject vfx = Instantiate(
            shotFeedbackVFX,
            playerVFXAnchor.position,
            playerVFXAnchor.rotation,
            playerVFXAnchor
        );

        // -------- CARTOON FX TEXT --------
        if (!vfx.TryGetComponent(out CFXR_ParticleText textFX) || !textFX.isDynamic)
        {
            Debug.LogWarning("Shot Feedback VFX must have Dynamic CFXR_ParticleText");
            Destroy(vfx);
            return;
        }

        string text;
        Color color;

        switch (aimSystem.CurrentShotQuality)
        {
            case ShotQuality.Perfect:
                text = "PERFECT!";
                color = Color.green;
                break;

            case ShotQuality.Sloppy:
                text = "SLOPPY!";
                color = Color.red;
                break;

            default:
                text = "OK";
                color = Color.yellow;
                break;
        }

        textFX.UpdateText(
            newText: text,
            newColor1: color,
            newColor2: color
        );

        Destroy(vfx, feedbackVfxLifetime);
    }
}
