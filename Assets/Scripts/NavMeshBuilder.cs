using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using Unity.AI.Navigation;

public class NavMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public ARPlaneManager planeManager;
    public NavMeshSurface navMeshSurface;

    [Header("Build Settings")]
    public float initialBuildDelay = 1.5f;
    public float rebuildCooldown = 1.0f;

    private float lastRebuildTime;
    private bool initialBuilt;
    void Awake()
    {
        if (!navMeshSurface)
            navMeshSurface = GetComponent<NavMeshSurface>();
    }
    
    IEnumerator Start()
    {
        // Wait for AR planes to appear
        yield return new WaitForSeconds(initialBuildDelay);
        BuildNavMesh();
        initialBuilt = true;
    }
    void Update()
    {
        if (!initialBuilt || planeManager == null)
            return;

        // If planes exist and cooldown passed ? rebuild
        if (Time.time - lastRebuildTime > rebuildCooldown && planeManager.trackables.count > 0)
            BuildNavMesh();
        
    }

    public void BuildNavMesh()
    {
        lastRebuildTime = Time.time;

        AssignPlanesToGroundLayer();
        navMeshSurface.BuildNavMesh();

        Debug.Log("Runtime NavMesh rebuilt");
    }
    void AssignPlanesToGroundLayer()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.layer = LayerMask.NameToLayer("Ground");

            if (!plane.TryGetComponent<MeshCollider>(out _))
                plane.gameObject.AddComponent<MeshCollider>();
        }
    }
}