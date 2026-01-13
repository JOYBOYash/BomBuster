using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BombProjectile : MonoBehaviour
{
    [Header("Explosion")]
    public float explosionRadius = 6f;
    public float explosionForce = 700f;
    public float damage = 100f;
    public LayerMask damageLayers;

    [Header("Explosion Visuals")]
    public GameObject explosionEffectPrefab;   // Assign in Inspector
    public float explosionExpandTime = 0.4f;   // Time to reach full radius

    [Header("Lifetime")]
    public float maxLifeTime = 6f;

    private Rigidbody rb;
    private Collider col;
    private bool hasExploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        Destroy(gameObject, maxLifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        StartCoroutine(ExplosionSequence(collision.contacts[0].point));
    }

    // ================= EXPLOSION SEQUENCE =================

    IEnumerator ExplosionSequence(Vector3 hitPoint)
    {
        hasExploded = true;

        // 1️⃣ Freeze bomb
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        col.enabled = false;

        // 2️⃣ Spawn explosion VFX
        GameObject explosionFX = null;

        if (explosionEffectPrefab != null)
        {
            explosionFX = Instantiate(
                explosionEffectPrefab,
                hitPoint,
                Quaternion.identity
            );

            explosionFX.transform.localScale = Vector3.zero;
        }

        // 3️⃣ Apply damage & force ONCE (at start or midpoint)
        ApplyExplosionDamage(hitPoint);

        // 4️⃣ Expand explosion visual
        float timer = 0f;
        while (timer < explosionExpandTime)
        {
            timer += Time.deltaTime;
            float t = timer / explosionExpandTime;

            if (explosionFX != null)
            {
                float scale = Mathf.Lerp(0f, explosionRadius * 2f, t);
                explosionFX.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // 5️⃣ Cleanup
        if (explosionFX != null)
            Destroy(explosionFX);

        Destroy(gameObject);
    }

    // ================= DAMAGE & FORCE =================

    void ApplyExplosionDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(
            center,
            explosionRadius,
            damageLayers
        );

        foreach (Collider hit in hits)
        {
            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(
                    explosionForce,
                    center,
                    explosionRadius
                );
            }

            if (hit.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
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
