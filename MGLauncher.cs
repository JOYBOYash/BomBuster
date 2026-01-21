using UnityEngine;
using CartoonFX;

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

    [Header("Burst / Reload")]
    public int bombsPerBurst = 3;
    public float reloadDuration = 1.2f;

    [Header("Momentum Transfer")]
    public float movementInfluence = 0.65f;
    public float maxMovementBoost = 12f;

    [Header("Camera Shake")]
    public float minShakeIntensity = 0.15f;
    public float maxShakeIntensity = 0.45f;
    public float shakeDuration = 0.35f;

    [Header("Shot Feedback VFX")]
    public Transform playerVFXAnchor;
    public GameObject shotFeedbackVFX;
    public float feedbackVfxLifetime = 1.5f;

    [Header("Shot Quality Thresholds")]
    [Range(0f, 1f)] public float perfectThreshold = 0.85f;
    [Range(0f, 1f)] public float sloppyThreshold = 0.3f;

    [Header("Dependencies")]
    public MGLPrecisionAim aimSystem;

    float nextFireTime;
    int shotsInBurst;
    bool isReloading;
    float reloadEndTime;

    // -------- VFX Pool --------
    GameObject feedbackInstance;
    CFXR_ParticleText cachedTextFX;
    ParticleSystem[] cachedParticles;
    float feedbackDisableTime;

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

    void Update()
    {
        if (isReloading && Time.time >= reloadEndTime)
        {
            isReloading = false;
            shotsInBurst = 0;
        }

        if (feedbackInstance != null &&
            feedbackInstance.activeSelf &&
            Time.time >= feedbackDisableTime)
        {
            feedbackInstance.SetActive(false);
        }
    }

    // ================= FIRING =================

    void HandleFireRequest()
    {
        if (aimSystem == null) return;
        if (isReloading) return;
        if (Time.time < nextFireTime) return;

        FireBomb();
        ShowShotFeedback();

        shotsInBurst++;
        nextFireTime = Time.time + fireRate;

        if (shotsInBurst >= bombsPerBurst)
        {
            isReloading = true;
            reloadEndTime = Time.time + reloadDuration;
        }
    }

    void FireBomb()
    {
        Vector3 spawnPos = firePoint.position + playerCamera.transform.forward * 0.5f;
        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Vector3 fireDir = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f
        ) * playerCamera.transform.forward;

        float precisionForce = baseLaunchForce * aimSystem.CurrentLaunchMultiplier;

        Vector3 playerVel = playerController != null ? playerController.CurrentVelocity : Vector3.zero;
        float forwardSpeed = Vector3.Dot(playerVel, fireDir.normalized);
        float movementBoost = Mathf.Clamp(forwardSpeed * movementInfluence, 0f, maxMovementBoost);

        rb.linearVelocity = fireDir * (precisionForce + movementBoost);

        if (bomb.TryGetComponent(out BombProjectile bombLogic))
        {
            bombLogic.SetChargeValues(
                aimSystem.CurrentExplosionRadius,
                aimSystem.CurrentDamageMultiplier
            );
        }

        float shake = Mathf.Lerp(minShakeIntensity, maxShakeIntensity, aimSystem.CurrentPrecision01);
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shake, shakeDuration);
    }

    // ================= SHOT FEEDBACK =================

    void ShowShotFeedback()
    {
        if (shotFeedbackVFX == null || playerVFXAnchor == null)
            return;

        if (feedbackInstance == null)
        {
            feedbackInstance = Instantiate(
                shotFeedbackVFX,
                playerVFXAnchor.position,
                playerVFXAnchor.rotation,
                playerVFXAnchor
            );

            cachedTextFX = feedbackInstance.GetComponent<CFXR_ParticleText>();
            cachedParticles = feedbackInstance.GetComponentsInChildren<ParticleSystem>(true);

            if (cachedTextFX == null || !cachedTextFX.isDynamic)
            {
                Debug.LogError("Shot Feedback VFX must use Dynamic CFXR_ParticleText");
                return;
            }

            feedbackInstance.SetActive(false);
        }

        float p = aimSystem.CurrentPrecision01;

        string text;
        Color color;

        if (p >= perfectThreshold)
        {
            text = "PERFECT!";
            color = Color.green;
        }
        else if (p <= sloppyThreshold)
        {
            text = "SLOPPY!";
            color = Color.red;
        }
        else
        {
            text = "OK";
            color = Color.yellow;
        }

        cachedTextFX.UpdateText(text, null, color, color);

        feedbackInstance.transform.SetPositionAndRotation(
            playerVFXAnchor.position,
            playerVFXAnchor.rotation
        );

        feedbackInstance.SetActive(true);

        // 🔥 FORCE RESTART PARTICLES (THIS WAS THE MISSING PIECE)
        foreach (var ps in cachedParticles)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        feedbackDisableTime = Time.time + feedbackVfxLifetime;
    }
}
