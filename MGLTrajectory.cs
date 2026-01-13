using UnityEngine;

public class MGLTrajectoryVisualizer : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public LineRenderer trajectoryLine;
    public LineRenderer explosionRadiusLine;

    [Header("Radius Colors")]
    public Color wideRadiusColor = Color.red;
    public Color midRadiusColor = Color.yellow;
    public Color smallRadiusColor = Color.green;

    [Tooltip("Threshold where radius is considered 'small / perfect'")]
    public float perfectThreshold = 0.8f;

    [Tooltip("Threshold where radius is considered 'medium'")]
    public float mediumThreshold = 0.4f;


    [Header("Dependencies")]
    public MGLOscillatingAim aimSystem;
    public MGLLauncher launcher;

    [Header("Trajectory")]
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;
    public LayerMask collisionLayers;

    [Header("Explosion Preview")]
    public int circleSegments = 32;

    [Header("Visual Feedback")]
    public Gradient radiusColorGradient;
    public float pulseSpeed = 6f;
    public float pulseScale = 0.12f;

    void Update()
    {
        if (!aimSystem.IsAiming)
        {
            trajectoryLine.enabled = false;
            explosionRadiusLine.enabled = false;
            return;
        }

        trajectoryLine.enabled = true;
        explosionRadiusLine.enabled = true;

        DrawTrajectory();
    }

    void DrawTrajectory()
    {
        trajectoryLine.positionCount = 0;
        explosionRadiusLine.positionCount = 0;

        Vector3 startPos = firePoint.position;
        Vector3 velocity =
            playerCamera.transform.forward *
            launcher.baseLaunchForce *
            aimSystem.CurrentLaunchMultiplier;

        Vector3 gravity = Physics.gravity;

        Vector3 prev = startPos;
        trajectoryLine.positionCount = 1;
        trajectoryLine.SetPosition(0, prev);

        for (int i = 1; i < trajectoryPoints; i++)
        {
            float t = i * timeStep;
            Vector3 next =
                startPos +
                velocity * t +
                0.5f * gravity * t * t;

            if (Physics.Raycast(prev, next - prev, out RaycastHit hit,
                Vector3.Distance(prev, next), collisionLayers))
            {
                trajectoryLine.positionCount++;
                trajectoryLine.SetPosition(trajectoryLine.positionCount - 1, hit.point);
                DrawExplosionRadius(hit.point, hit.normal);
                return;
            }

            trajectoryLine.positionCount++;
            trajectoryLine.SetPosition(trajectoryLine.positionCount - 1, next);
            prev = next;
        }
    }

    void DrawExplosionRadius(Vector3 center, Vector3 normal)
    {
        explosionRadiusLine.positionCount = circleSegments;

        // 🔥 Use INVERTED value (small radius = high skill)
        float inverted = 1f - aimSystem.CurrentCharge01;

        // -------- COLOR SELECTION --------
        Color targetColor;

        if (inverted >= perfectThreshold)
            targetColor = smallRadiusColor;      // 🟢 MAX DAMAGE
        else if (inverted >= mediumThreshold)
            targetColor = midRadiusColor;        // 🟡 MEDIUM
        else
            targetColor = wideRadiusColor;       // 🔴 LOW DAMAGE

        // -------- APPLY COLOR TO MATERIAL --------
        if (explosionRadiusLine.material != null)
        {
            explosionRadiusLine.material.color = targetColor;
        }

        // -------- PULSE (STRONGER NEAR PERFECT) --------
        float pulseStrength = Mathf.InverseLerp(
            mediumThreshold,
            perfectThreshold,
            inverted
        );

        float pulse =
            Mathf.Sin(Time.time * pulseSpeed) *
            pulseScale *
            pulseStrength;

        explosionRadiusLine.widthMultiplier = 1f + pulse;

        // -------- DRAW CIRCLE --------
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        float r = aimSystem.CurrentExplosionRadius;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i / (float)circleSegments * Mathf.PI * 2f;
            Vector3 local = new Vector3(
                Mathf.Cos(angle) * r,
                0f,
                Mathf.Sin(angle) * r
            );

            explosionRadiusLine.SetPosition(i, center + rot * local);
        }
    }

}
