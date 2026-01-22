using UnityEngine;
using System.Collections.Generic;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;

    [Header("Live Data (Instructor Setup)")]
    // The Master Lists
    public List<GameObject> activeEnemies = new List<GameObject>();
    public List<GameObject> activeObstacles = new List<GameObject>();

    // The Room Geometry
    public List<Vector3> zonePoints = new List<Vector3>(); // From ZoneTool
    public Vector3 breachPointPosition;                    // From CoordinateMarker

    [Header("Runtime Data")]
    public string lastSessionFolderPath; // Path for AAR to load later

    void Awake()
    {
        Instance = this;
    }

    // =========================================================
    // 1. REGISTRATION (Called by Instructor Tools)
    // =========================================================

    // --- ENEMIES ---
    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy)) activeEnemies.Add(enemy);
    }
    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy)) activeEnemies.Remove(enemy);
    }

    // --- OBSTACLES ---
    public void RegisterObstacle(GameObject obs)
    {
        if (!activeObstacles.Contains(obs)) activeObstacles.Add(obs);
    }
    public void UnregisterObstacle(GameObject obs)
    {
        if (activeObstacles.Contains(obs)) activeObstacles.Remove(obs);
    }

    // --- GEOMETRY (Breach & Zone) ---
    public void SetBreachPoint(Vector3 pos)
    {
        breachPointPosition = pos;
        Debug.Log($"[SessionManager] Breach Point Set: {pos}");
    }

    public void SetZonePoints(List<Vector3> points)
    {
        zonePoints = new List<Vector3>(points); // Create a copy
        Debug.Log($"[SessionManager] Zone Boundary Set: {points.Count} points");
    }

    // =========================================================
    // 2. TRAINEE PREP (The "Wake Up")
    // =========================================================
    public void PrepareForTrainee()
    {
        // We do NOT toggle colliders. We just ensure scripts are reset.
        foreach (var enemy in activeEnemies)
        {
            if (enemy == null) continue;

            var behavior = enemy.GetComponent<TargetBehavior>();
            if (behavior != null) behavior.ResetTarget(); // Revive them if they were shot in a previous test
        }
    }

    // =========================================================
    // 3. SAVING (The Packer)
    // =========================================================
    public SessionManifest GenerateManifest()
    {
        SessionManifest manifest = new SessionManifest();
        manifest.sessionID = System.Guid.NewGuid().ToString();

        // 1. Save Geometry
        manifest.breachPoint = breachPointPosition;
        manifest.zoneBounds = new List<Vector3>(zonePoints);

        // 2. Save Enemies
        foreach (var obj in activeEnemies)
        {
            if (obj == null) continue;
            manifest.enemies.Add(new EntityData
            {
                id = obj.name,
                type = "Target",
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale
            });
        }

        // 3. Save Obstacles
        foreach (var obj in activeObstacles)
        {
            if (obj == null) continue;
            manifest.obstacles.Add(new EntityData
            {
                id = obj.name,
                type = "Obstacle",
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale
            });
        }

        return manifest;
    }
}