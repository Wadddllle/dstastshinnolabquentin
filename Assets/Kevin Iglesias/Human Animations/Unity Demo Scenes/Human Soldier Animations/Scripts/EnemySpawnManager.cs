using UnityEngine;

public class EnemySpawnManager : MonoBehaviour  
{
    private GameObject[] spawnPoints;
    public GameObject enemyPrefab;
    public EnemyConfig enemyConfig;

    [Range(0f, 1f)]
    public float chance = 0.5f;

    void Start()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
        foreach (GameObject spawnPoint in spawnPoints)
        {
            float poll = Random.value;
            if (poll <= chance)
            {
                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.transform.position, Quaternion.identity);

                // 3. Give it a Unique Name (Critical for JSON saving)
                enemy.name = $"Target_{System.DateTime.Now.Ticks % 10000}"; // e.g., Target_4921

                // 4. REGISTER WITH DATABASE
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.RegisterEnemy(enemy);
                    Debug.Log($"[Instructor] Enemy Placed & Registered: {enemy.name}");
                }
                else
                    Debug.LogError("[Instructor] CRITICAL: SessionManager not found! Enemy not saved.");
                
                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
                if (enemyAI != null && enemyConfig != null)
                    enemyAI.ApplyConfig(enemyConfig);
            }
        }
    }
}
