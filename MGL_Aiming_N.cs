using UnityEngine;
using UnityEngine.InputSystem;
using CartoonFX;

public class MGLPrecisionAim : MonoBehaviour
{
    [Header("Precision Aim")]
    public float focusSpeed = 1.6f;
    public float instabilityPenalty = 2.4f;
    public float instabilityThreshold = 1.8f;

    [Header("Explosion Radius")]
    public float minExplosionRadius = 1.2f; // perfect
    public float maxExplosionRadius = 6f;   // sloppy

    [Header("Force / Damage")]
    public float minLaunchForceMultiplier = 0.6f;
    public float maxLaunchForceMultiplier = 1.4f;

    public float minDamageMultiplier = 0.6f;
    public float maxDamageMultiplier = 1.4f;

    [Header("Shot Quality Thresholds")]
    [Range(0f, 1f)]
    public float perfectShotThreshold = 0.85f;

    [Range(0f, 1f)]
    public float sloppyShotThreshold = 0.3f;

   

public ShotQuality CurrentShotQuality { get; private set; }


    // ================= NEW: SHOT FEEDBACK VFX =================

    [Header("Shot Feedback VFX")]
    [Tooltip("Cartoon FX Particle Text (must be Dynamic)")]


    public string perfectShotText = "PERFECT";
    public string sloppyShotText = "SLOPPY";

    // ==========================================================

    public bool IsAiming { get; private set; }

    // ================= EVENTS =================

    public System.Action OnFireRequested;
    // ================= RUNTIME VALUES =================

    public float CurrentExplosionRadius { get; private set; }
    public float CurrentLaunchMultiplier { get; private set; }
    public float CurrentDamageMultiplier { get; private set; }

    [Range(0f, 1f)]
    public float CurrentPrecision01 { get; private set; } // 1 = perfect

    // Internal
    Vector2 lastMouseDelta;
    InputAction lookAction;
    InputAction fireAction;

    void Awake()
    {
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
    }

    void OnEnable()
    {
        lookAction.Enable();
        fireAction.Enable();

        fireAction.started += OnAimStarted;
        fireAction.canceled += OnAimReleased;
    }

    void OnDisable()
    {
        fireAction.started -= OnAimStarted;
        fireAction.canceled -= OnAimReleased;

        lookAction.Disable();
        fireAction.Disable();
    }

    void Update()
    {
        if (!IsAiming)
            return;

        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        float instability = mouseDelta.magnitude;

        // ================= PRECISION LOGIC =================

        if (instability < instabilityThreshold)
            CurrentPrecision01 += focusSpeed * Time.deltaTime;
        else
            CurrentPrecision01 -= instabilityPenalty * Time.deltaTime;

        CurrentPrecision01 = Mathf.Clamp01(CurrentPrecision01);

        // ================= MAP TO GAMEPLAY =================

        CurrentExplosionRadius = Mathf.Lerp(
            maxExplosionRadius,
            minExplosionRadius,
            CurrentPrecision01
        );

        CurrentLaunchMultiplier = Mathf.Lerp(
            minLaunchForceMultiplier,
            maxLaunchForceMultiplier,
            CurrentPrecision01
        );

        CurrentDamageMultiplier = Mathf.Lerp(
            minDamageMultiplier,
            maxDamageMultiplier,
            CurrentPrecision01
        );

        lastMouseDelta = mouseDelta;
    }

    // ================= INPUT =================

    void OnAimStarted(InputAction.CallbackContext ctx)
    {
        IsAiming = true;
        CurrentPrecision01 = 0.5f; // neutral start
    }

   void OnAimReleased(InputAction.CallbackContext ctx)
{
    if (!IsAiming)
        return;

    IsAiming = false;

    // -------- DETERMINE SHOT QUALITY --------
    if (CurrentPrecision01 >= perfectShotThreshold)
        CurrentShotQuality = ShotQuality.Perfect;
    else if (CurrentPrecision01 <= sloppyShotThreshold)
        CurrentShotQuality = ShotQuality.Sloppy;
    else
        CurrentShotQuality = ShotQuality.Normal;

    // -------- FIRE --------
    OnFireRequested?.Invoke();
}

}
