using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    new Collider collider;
    private bool useAlr;
    public GameObject enemyPrefab;
    public EnemyConfig enemyConfig;
    private float poll;

    [Range(0f, 1f)]
    public float chance = 0.5f;
    void Start()
    {
        collider = GetComponent<Collider>();
    }
    // Update is called once per frame
    void Update()
    {
        if (AppManager.Instance.IsInstructorState())
        {
            collider.enabled = true;
            if (useAlr == true)
                useAlr = false;

        }  
        else if (AppManager.Instance.IsTraineeState())
        {
            collider.enabled = false;

            if (useAlr == false)
            {
                useAlr = true;
                poll = Random.value;
                if (poll <= chance)
                {
                    GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);

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
}
