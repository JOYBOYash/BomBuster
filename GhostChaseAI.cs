using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GhostAI : MonoBehaviour
{
    [Header("Targeting")]
    public string playerTag = "Player";
    public float stopDistance = 2.5f;

    [Header("Movement")]
    public float moveSpeed = 2.8f;
    public float rotationSpeed = 8f;

    [Header("Separation (Anti-Overlap)")]
    public float separationRadius = 1.2f;
    public float separationStrength = 3.5f;
    public LayerMask ghostLayer;

    [Header("Optimization")]
    public float playerSearchInterval = 1f;

    Transform player;
    float searchTimer;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        searchTimer -= Time.deltaTime;
        if (player == null && searchTimer <= 0f)
        {
            FindPlayer();
        }

        if (player == null)
            return;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;

        // -------- ROTATION --------
        if (toPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(toPlayer.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // -------- MOVEMENT --------
        if (distance > stopDistance)
        {
            Vector3 moveDir = toPlayer.normalized;

            // Apply separation force
            moveDir += CalculateSeparation();

            transform.position +=
                moveDir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    Vector3 CalculateSeparation()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            separationRadius,
            ghostLayer,
            QueryTriggerInteraction.Ignore
        );

        Vector3 separation = Vector3.zero;
        int count = 0;

        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            Vector3 away = transform.position - hit.transform.position;
            float dist = away.magnitude;

            if (dist > 0f)
            {
                separation += away.normalized / dist;
                count++;
            }
        }

        if (count > 0)
        {
            separation /= count;
            separation *= separationStrength;
        }

        return separation;
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
            player = p.transform;

        searchTimer = playerSearchInterval;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
    }
}
