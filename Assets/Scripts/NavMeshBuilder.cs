using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using Unity.AI.Navigation;

public class NavMeshBuilder : MonoBehaviour
{
    [Header("References")]
    public NavMeshSurface navMeshSurface;
    public ChunkManager chunkManager;

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
        if (!initialBuilt || chunkManager == null)
            return;

        // If planes exist and cooldown passed ? rebuild
        if (Time.time - lastRebuildTime > rebuildCooldown)
            BuildNavMesh();
        
    }

    public void BuildNavMesh()
    {
        lastRebuildTime = Time.time;

        AssignChunksToFloorLayer();
        navMeshSurface.BuildNavMesh();

        Debug.Log("Runtime NavMesh rebuilt");
    }

    void AssignChunksToFloorLayer()
    {
        foreach (var chunkPair in chunkManager.GetActiveChunks())
        {
            var chunk = chunkPair.Value.instance;
            chunk.gameObject.layer = LayerMask.NameToLayer("Floor");

            MeshCollider navCollider = chunk.GetComponentInChildren<MeshCollider>();
            if (navCollider != null)
                navCollider.gameObject.layer = LayerMask.NameToLayer("Floor");
        }
    }
    /*void AssignPlanesToFloorLayer()
    {

        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.layer = LayerMask.NameToLayer("Floor");

            if (!plane.TryGetComponent<MeshCollider>(out _))
                plane.gameObject.AddComponent<MeshCollider>();
        }
    }*/
}