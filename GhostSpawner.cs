using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    public GameObject ghostPrefab;
    public int spawnCount = 10;
    public float spawnRadius = 15f;

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnGhost();
        }
    }

    void SpawnGhost()
    {
        Vector3 randomPos = transform.position +
            Random.insideUnitSphere * spawnRadius;

        randomPos.y = transform.position.y;

        Instantiate(ghostPrefab, randomPos, Quaternion.identity);
    }
}
