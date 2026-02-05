using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    protected float currentHealth;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
