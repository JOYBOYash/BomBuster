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

    [Header("Default Explosion Timing")]
    public float defaultExpandTime = 0.4f;
    public float defaultBombHideDelay = 0.05f;

    [Header("Explosion Effects")]
    public GameObject defaultExplosionEffect;
    public ExplosionEffectRule[] layerExplosionEffects;

    [Header("Screen Shake")]
    public float shakeIntensity = 0.35f;
    public float shakeDuration = 0.4f;

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

        // Stop physics immediately
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        col.enabled = false;

        // Resolve layer-based config
        SelectExplosionConfig(
            hitCollider,
            out GameObject effectPrefab,
            out float expandTime,
            out float hideDelay
        );

        // Spawn explosion effect (independent object)
        GameObject explosionFX = null;
        if (effectPrefab != null)
        {
            explosionFX = Instantiate(effectPrefab, hitPoint, Quaternion.identity);
            explosionFX.transform.localScale = Vector3.zero;
        }

        // Apply gameplay effects once
        ApplyExplosionDamage(hitPoint);
        CameraShake.Shake(shakeIntensity, shakeDuration);

        // Hide bomb visuals AFTER delay (not destroy)
        yield return new WaitForSeconds(hideDelay);
        HideBombVisuals();

        // Animate explosion expansion
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

    // ================= DAMAGE =================

    void ApplyExplosionDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, explosionRadius, damageLayers);

        foreach (Collider hit in hits)
        {
            float distance = Vector3.Distance(center, hit.transform.position);
            float falloff = 1f - Mathf.Clamp01(distance / explosionRadius);

            float damage = maxDamage * falloff;

            if (hit.TryGetComponent(out Health health))
                health.TakeDamage(damage);

            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(
                    explosionForce * falloff,
                    center,
                    explosionRadius
                );
            }
        }
    }

    // ================= VISUAL CONTROL =================

    void HideBombVisuals()
    {
        foreach (var r in renderers)
            r.enabled = false;
    }

    // ================= CONFIG SELECTION =================

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
    }
}
