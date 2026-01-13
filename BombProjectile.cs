using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BombProjectile : MonoBehaviour
{
    [Header("Explosion")]
    public float explosionRadius = 6f;
    public float explosionForce = 700f;
    public float damage = 100f;
    public LayerMask damageLayers;

    [Header("Lifetime")]
    public float maxLifeTime = 6f;



    private Rigidbody rb;
    private bool hasExploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, maxLifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        Explode();
    }

    void Explode()
    {
        hasExploded = true;

        // Detect objects in radius
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            damageLayers
        );

        foreach (Collider hit in hits)
        {
            // Apply force
            Rigidbody hitRb = hit.attachedRigidbody;
            if (hitRb != null)
            {
                hitRb.AddExplosionForce(
                    explosionForce,
                    transform.position,
                    explosionRadius
                );
            }

            // Apply damage (optional)
            if (hit.TryGetComponent(out Health health))
            {
                health.TakeDamage(damage);
            }
        }

        // Optional VFX
        // Instantiate(explosionVFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    // Debug radius
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
