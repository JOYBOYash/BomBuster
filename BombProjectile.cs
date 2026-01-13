using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BombProjectile : MonoBehaviour
{
    [System.Serializable]
    public struct ExplosionEffectRule
    {
        public LayerMask layer;
        public GameObject explosionEffect;
        public float expandTime;
        public float bombHideDelay;
    }

    [Header("Explosion")]
    public float explosionRadius = 6f;
    public float explosionForce = 700f;
    public float maxDamage = 100f;
    public LayerMask damageLayers;

    [Header("Shockwave")]
    public float shockwaveMultiplier = 1.5f;
    public float shockwaveDuration = 0.35f;
    public int shockwaveSteps = 12;

    [Header("Default Explosion Timing")]
    public float defaultExpandTime = 0.4f;
    public float defaultBombHideDelay = 0.05f;

    [Header("Explosion Effects")]
    public GameObject defaultExplosionEffect;
    public ExplosionEffectRule[] layerExplosionEffects;

    [Header("Camera Shake (Explosion Impact)")]
    public float minShakeIntensity = 0.25f;
    public float maxShakeIntensity = 0.6f;
    public float shakeDuration = 0.45f;

    private Rigidbody rb;
    private Collider col;
    private Renderer[] renderers;
    private bool hasExploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        StartCoroutine(ExplosionSequence(collision.contacts[0].point, collision.collider));
    }

    // ================= EXPLOSION SEQUENCE =================

    IEnumerator ExplosionSequence(Vector3 hitPoint, Collider hitCollider)
    {
        hasExploded = true;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        col.enabled = false;

        SelectExplosionConfig(
            hitCollider,
            out GameObject effectPrefab,
            out float expandTime,
            out float hideDelay
        );

        GameObject explosionFX = null;
        if (effectPrefab != null)
        {
            explosionFX = Instantiate(effectPrefab, hitPoint, Quaternion.identity);
            explosionFX.transform.localScale = Vector3.zero;
        }

        // ---------------- GAMEPLAY EFFECTS ----------------
        ApplyExplosionDamage(hitPoint);
        StartCoroutine(ShockwaveRoutine(hitPoint));

        // ---------------- CAMERA SHAKE ----------------
        TriggerExplosionShake(hitPoint);

        yield return new WaitForSeconds(hideDelay);
        HideBombVisuals();

        float timer = 0f;
        while (timer < expandTime)
        {
            timer += Time.deltaTime;
            float t = timer / expandTime;

            if (explosionFX != null)
            {
                float scale = Mathf.Lerp(0f, explosionRadius * 2f, t);
                explosionFX.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (explosionFX != null)
            Destroy(explosionFX);

        Destroy(gameObject);
    }

    // ================= CAMERA SHAKE =================

    void TriggerExplosionShake(Vector3 explosionPoint)
    {
        if (CameraShake.Instance == null)
            return;

        // Optional distance-based attenuation (player far = weaker shake)
        float distance = Vector3.Distance(
            CameraShake.Instance.transform.position,
            explosionPoint
        );

        float distanceFactor = Mathf.Clamp01(1f - (distance / (explosionRadius * 3f)));

        float finalShake = Mathf.Lerp(
            minShakeIntensity,
            maxShakeIntensity,
            distanceFactor
        );

        CameraShake.Instance.Shake(finalShake, shakeDuration);
    }

    // ================= SHOCKWAVE =================

    IEnumerator ShockwaveRoutine(Vector3 center)
    {
        float maxShockRadius = explosionRadius * shockwaveMultiplier;
        float stepTime = shockwaveDuration / shockwaveSteps;

        float previousRadius = 0f;

        for (int i = 1; i <= shockwaveSteps; i++)
        {
            float currentRadius = (i / (float)shockwaveSteps) * maxShockRadius;

            Collider[] hits = Physics.OverlapSphere(
                center,
                currentRadius,
                damageLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                Rigidbody hitRb = hit.attachedRigidbody;
                if (hitRb == null) continue;

                Vector3 closestPoint = hit.ClosestPoint(center);
                float distance = Vector3.Distance(center, closestPoint);

                if (distance <= previousRadius || distance > currentRadius)
                    continue;

                float normalized = distance / maxShockRadius;
                float forceFactor = Mathf.Clamp01(1f - normalized);

                hitRb.AddExplosionForce(
                    explosionForce * forceFactor,
                    center,
                    maxShockRadius,
                    0f,
                    ForceMode.Impulse
                );
            }

            previousRadius = currentRadius;
            yield return new WaitForSeconds(stepTime);
        }
    }

    // ================= DAMAGE =================

    void ApplyExplosionDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            explosionRadius,
            damageLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            Vector3 closestPoint = hit.ClosestPoint(center);
            float distance = Vector3.Distance(center, closestPoint);

            if (distance > explosionRadius)
                continue;

            float falloff = 1f - (distance / explosionRadius);
            float damage = maxDamage * falloff;

            if (hit.TryGetComponent(out Health health))
                health.TakeDamage(damage);

            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(
                    explosionForce * falloff,
                    center,
                    explosionRadius,
                    0f,
                    ForceMode.Impulse
                );
            }
        }
    }

    // ================= CHARGE DATA =================

    public void SetChargeValues(float radius, float damageMultiplier)
    {
        explosionRadius = radius;
        maxDamage *= damageMultiplier;
    }

    // ================= VISUAL =================

    void HideBombVisuals()
    {
        foreach (var r in renderers)
            r.enabled = false;
    }

    // ================= CONFIG =================

    void SelectExplosionConfig(
        Collider hitCollider,
        out GameObject effect,
        out float expandTime,
        out float hideDelay
    )
    {
        effect = defaultExplosionEffect;
        expandTime = defaultExpandTime;
        hideDelay = defaultBombHideDelay;

        foreach (var rule in layerExplosionEffects)
        {
            if ((rule.layer.value & (1 << hitCollider.gameObject.layer)) != 0)
            {
                effect = rule.explosionEffect;
                expandTime = rule.expandTime;
                hideDelay = rule.bombHideDelay;
                return;
            }
        }
    }

    // ================= DEBUG =================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius * shockwaveMultiplier);
    }
}
