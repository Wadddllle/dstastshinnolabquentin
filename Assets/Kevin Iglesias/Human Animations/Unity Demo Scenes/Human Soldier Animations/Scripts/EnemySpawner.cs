using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    public Transform player;
    public GameObject enemyPrefab;
    public GameObject spawnParticlePrefab;
    public float spawnInterval = 5f;
    public float spawnInnerRadius = 30f;
    public float spawnOuterRadius = 20f;
    public float spawnerDelay = 5f;
    public float minSpawnInterval = 2f;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(spawnerDelay);
        StartCoroutine(SpawnEnemies());
        StartCoroutine(ShortenSpawnInterval());
    }

    IEnumerator SpawnEnemies()
    {
        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }

    }

    IEnumerator ShortenSpawnInterval()
    {
        while (spawnInterval >= minSpawnInterval)
        {
            yield return new WaitForSeconds(spawnerDelay);
            spawnInterval -= 0.5f;
        }
    }
    
    void SpawnEnemy() //spawn in random position on navmesh within donut demarcated by inner and outer radial limits
    {
        Vector3 spawnPosition = player.position + (Vector3)Random.insideUnitCircle.normalized * Random.Range(spawnInnerRadius, spawnOuterRadius);
        if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            
            Instantiate(enemyPrefab, hit.position, Quaternion.identity);
            GameObject spawnParticle = Instantiate(spawnParticlePrefab, hit.position, Quaternion.identity);
        }
    }
}
