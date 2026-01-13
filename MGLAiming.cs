using UnityEngine;
using UnityEngine.InputSystem;

public class MGLOscillatingAim : MonoBehaviour
{
    [Header("Oscillation")]
    public float oscillationDuration = 1.2f;

    [Header("Explosion Radius")]
    public float minExplosionRadius = 1.2f;
    public float maxExplosionRadius = 6f;

    [Header("Force / Damage")]
    public float minLaunchForceMultiplier = 0.6f;
    public float maxLaunchForceMultiplier = 1.4f;

    public float minDamageMultiplier = 0.6f;
    public float maxDamageMultiplier = 1.4f;

    public bool IsAiming { get; private set; }

    public float CurrentExplosionRadius { get; private set; }
    public float CurrentLaunchMultiplier { get; private set; }
    public float CurrentDamageMultiplier { get; private set; }
    public float CurrentCharge01 { get; private set; }

    [Header("Perfect Timing Assist")]
    [Tooltip("How close to perfect before slow-down kicks in")]
    public float perfectWindowThreshold = 0.85f;

    [Tooltip("Time scale at perfect timing (0.8 = 20% slow)")]
    public float slowTimeScale = 0.85f;

    [Tooltip("How fast time returns to normal")]
    public float timeRecoverSpeed = 6f;


    private float oscillationTimer;
    private InputAction fireAction;

    void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        fireAction.Enable();
        fireAction.started += OnAimStarted;
    }

    void OnDisable()
    {
        fireAction.started -= OnAimStarted;
        fireAction.Disable();
    }

    void Update()
    {
        if (!IsAiming)
        {
            // Recover time scale when not aiming
            Time.timeScale = Mathf.Lerp(
                Time.timeScale,
                1f,
                Time.unscaledDeltaTime * timeRecoverSpeed
            );
            return;
        }

        oscillationTimer += Time.unscaledDeltaTime;

        // 0 → 1 → 0 → 1
        CurrentCharge01 = Mathf.PingPong(
            oscillationTimer / oscillationDuration,
            1f
        );

        // inverted = 1 when SMALL radius (perfect)
        float inverted = 1f - CurrentCharge01;

        CurrentExplosionRadius = Mathf.Lerp(
            minExplosionRadius,
            maxExplosionRadius,
            inverted
        );

        CurrentLaunchMultiplier = Mathf.Lerp(
            minLaunchForceMultiplier,
            maxLaunchForceMultiplier,
            inverted
        );

        CurrentDamageMultiplier = Mathf.Lerp(
            minDamageMultiplier,
            maxDamageMultiplier,
            inverted
        );

        // ---------------- PERFECT TIMING SLOWDOWN ----------------
        if (inverted >= perfectWindowThreshold)
        {
            float t = Mathf.InverseLerp(
                perfectWindowThreshold,
                1f,
                inverted
            );

            float targetTimeScale = Mathf.Lerp(
                1f,
                slowTimeScale,
                t
            );

            Time.timeScale = targetTimeScale;
        }
        else
        {
            Time.timeScale = Mathf.Lerp(
                Time.timeScale,
                1f,
                Time.unscaledDeltaTime * timeRecoverSpeed
            );
        }
    }

    void OnAimStarted(InputAction.CallbackContext ctx)
    {
        IsAiming = true;
        oscillationTimer = 0f;
    }

    public void StopAiming()
    {
        IsAiming = false;
        Time.timeScale = 1f; // restore immediately on fire
    }

}
