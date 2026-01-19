using UnityEngine;

public class MGLTrajectoryVisualizer : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public LineRenderer trajectoryLine;
    public LineRenderer explosionRadiusLine;

    [Header("Dependencies")]
    public MGLPrecisionAim aimSystem;
    public MGLLauncher launcher;

    [Header("Trajectory")]
    public int trajectoryPoints = 30;
    public float timeStep = 0.1f;
    public LayerMask collisionLayers;

    [Header("Explosion Preview")]
    public int circleSegments = 48;

    [Header("Visual Style")]
    public float trajectoryScrollSpeed = 1.8f;
    public float circleScrollSpeed = 1.2f;

    [Header("Radius Colors")]
    public Color wideRadiusColor = Color.red;
    public Color midRadiusColor = Color.yellow;
    public Color smallRadiusColor = Color.green;

    [Header("Precision Thresholds")]
    public float perfectThreshold = 0.8f;
    public float mediumThreshold = 0.4f;

    [Header("Pulse")]
    public float pulseSpeed = 6f;
    public float pulseScale = 0.15f;

    Material trajMat;
    Material circleMat;

    void Awake()
    {
        trajMat = trajectoryLine.material;
        circleMat = explosionRadiusLine.material;
    }

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

        AnimateMaterials();
        DrawTrajectory();
    }

    void AnimateMaterials()
    {
        if (trajMat != null)
            trajMat.mainTextureOffset -= Vector2.right * Time.deltaTime * trajectoryScrollSpeed;

        if (circleMat != null)
            circleMat.mainTextureOffset -= Vector2.right * Time.deltaTime * circleScrollSpeed;
    }

    void DrawTrajectory()
    {
        trajectoryLine.positionCount = 0;
        explosionRadiusLine.positionCount = 0;

        Vector3 start = firePoint.position;
        Vector3 velocity =
            playerCamera.transform.forward *
            launcher.baseLaunchForce *
            aimSystem.CurrentLaunchMultiplier;

        Vector3 gravity = Physics.gravity;

        Vector3 prev = start;
        trajectoryLine.positionCount = 1;
        trajectoryLine.SetPosition(0, prev);

        for (int i = 1; i < trajectoryPoints; i++)
        {
            float t = i * timeStep;
            Vector3 next = start + velocity * t + 0.5f * gravity * t * t;

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
        float precision = aimSystem.CurrentPrecision01;

        Color color =
            precision >= perfectThreshold ? smallRadiusColor :
            precision >= mediumThreshold ? midRadiusColor :
            wideRadiusColor;

        if (circleMat != null)
            circleMat.color = color;

        float pulse =
            Mathf.Sin(Time.time * pulseSpeed) *
            pulseScale *
            precision;

        explosionRadiusLine.widthMultiplier = 1f + pulse;
        explosionRadiusLine.positionCount = circleSegments + 1;

        Quaternion rot = Quaternion.FromToRotation(Vector3.up, normal);
        float r = aimSystem.CurrentExplosionRadius;

        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i / (float)circleSegments * Mathf.PI * 2f;
            Vector3 local = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            explosionRadiusLine.SetPosition(i, center + rot * local);
        }
    }
}
