using UnityEngine;

public class MobSpawner : MonoBehaviour
{
    public GameObject mobPrefab;
    public Transform[] spawnPoints;
    public float spawnInterval = 3f;
    public int maxMobs = 10;

    private float timer;
    private int currentMobs;
    private int lastSpawnPointIndex = -1;

    void Update()
    {
        if (currentMobs >= maxMobs) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnMob();
            timer = 0f;
        }
    }

    void SpawnMob()
    {
        if (spawnPoints.Length == 0) return;

        lastSpawnPointIndex = (lastSpawnPointIndex + 1) % spawnPoints.Length;
        Transform spawnPoint = spawnPoints[lastSpawnPointIndex];

        Instantiate(mobPrefab, spawnPoint.position, spawnPoint.rotation);
        currentMobs++;
    }
}