using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class GhostHealth : Health
{
    [Header("Death")]
    [Tooltip("Fallback destroy delay if no audio clip is assigned")]
    public float fallbackDestroyDelay = 0.5f;

    [Header("Audio")]
    public AudioClip deathClip;
    [Range(0f, 1f)] public float deathVolume = 0.9f;

    bool isDead;
    AudioSource audioSource;
    Collider col;
    GhostAI ghostAI;

    protected override void Awake()
    {
        base.Awake();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound

        col = GetComponent<Collider>();
        ghostAI = GetComponent<GhostAI>();
    }

    public override void TakeDamage(float damage)
    {
        if (isDead)
            return;

        base.TakeDamage(damage);
    }

    protected override void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // -------- STOP INTERACTION IMMEDIATELY --------
        if (col != null)
            col.enabled = false;

        if (ghostAI != null)
            ghostAI.enabled = false;

        // -------- PLAY DEATH SEQUENCE --------
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        float delay = fallbackDestroyDelay;

        if (deathClip != null)
        {
            audioSource.PlayOneShot(deathClip, deathVolume);
            delay = deathClip.length;
        }

        yield return new WaitForSeconds(delay);

        Destroy(gameObject);
    }
}
