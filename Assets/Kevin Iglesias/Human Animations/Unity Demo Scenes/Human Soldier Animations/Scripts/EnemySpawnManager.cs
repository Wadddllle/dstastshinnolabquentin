using UnityEngine;

public class EnemySpawnManager : MonoBehaviour  
{
    private GameObject[] spawnPoints;
    public GameObject enemyPrefab;
    public EnemyConfig enemyConfig;

    [Range(0f, 1f)]
    public float chance = 0.5f;
    void Awake()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
    }
    void Start()
    {
        foreach (GameObject spawnPoint in spawnPoints)
        {
            float poll = Random.value;
            if (poll <= chance)
            {
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null && enemyConfig != null)
                    enemyAI.ApplyConfig(enemyConfig);
            }
        }
    }
}
