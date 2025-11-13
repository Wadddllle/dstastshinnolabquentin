//using UnityEngine;
//using System.Threading.Tasks;
//using Unity.Collections;
//using NaiveSurfaceNets; // The correct namespace
//using UnityEngine.Rendering;
//using Anaglyph.XRTemplate;
//using Unity.Burst;
//using Unity.Jobs;
//using Unity.Mathematics;

//[BurstCompile]
//public struct ScaleVerticesJob : IJobParallelFor
//{
//    // --- Inputs ---
//    [ReadOnly] public NativeArray<Vertex> RawVertices;
//    public float MetersPerVoxel;

//    // --- Outputs ---
//    // We will write the scaled positions and original normals into separate arrays.
//    [WriteOnly] public NativeArray<float3> FinalVertices;
//    [WriteOnly] public NativeArray<float3> FinalNormals;

//    public void Execute(int index)
//    {
//        // Get the original vertex data
//        Vertex rawVertex = RawVertices[index];

//        // Scale the position and write it to the output array
//        FinalVertices[index] = rawVertex.position * MetersPerVoxel;

//        // Copy the normal (it doesn't need scaling)
//        FinalNormals[index] = rawVertex.normal;
//    }
//}
//[RequireComponent(typeof(VoxelProvider))]
//public class MultiChunkTester : MonoBehaviour
//{
//    [Header("Dependencies")]
//    [SerializeField] private EnvironmentMapper _environmentMapper;
//    [SerializeField] private VoxelProvider _voxelProvider;

//    [Header("Assets")]
//    [SerializeField] private Material _meshMaterial;

//    private System.Collections.Generic.List<GameObject> _chunkGameObjects = new();

//    // From NaiveSurfaceNets Chunk.cs, this is a hard constraint.
//    private readonly Vector3Int _chunkDimensions = new Vector3Int(Chunk.ChunkSize, Chunk.ChunkSize, Chunk.ChunkSize);

//    void Start()
//    {
//        // --- ROBUST SAFETY CHECKS ---
//        if (_environmentMapper == null)
//            Debug.LogError("FATAL: Environment Mapper is not assigned in the Inspector!", this);
//        if (_voxelProvider == null)
//            Debug.LogError("FATAL: Voxel Provider is not assigned in the Inspector!", this);
//        if (_meshMaterial == null)
//            Debug.LogWarning("Mesh Material is not assigned. Chunks will be magenta.");

//        Debug.Log("MultiChunkTester (for NaiveSurfaceNets) Ready. Press 'M' to generate a stack of 4 chunks.");
//        // We'll wait for a key press instead of auto-running to give you control.
//        InvokeRepeating("RunMeshingTest", 1f, 1f);

//    }

//    void Update()
//    {
//        //if (Input.GetKeyDown(KeyCode.M))
//        //{
//        //    // Use a fire-and-forget async wrapper so Update doesn't get blocked
//        //    _ = RunMeshingTest();
//        //}
//    }
//    [ContextMenu("Run Meshing Test")]
//    private async Task RunMeshingTest()
//    {
//        Debug.Log("<color=cyan>--- Starting Multi-Chunk Meshing Test ---</color>");

//        foreach (var go in _chunkGameObjects) { Destroy(go); }
//        _chunkGameObjects.Clear();

//        // --- CORRECTED DATA ACCESS ---
//        var globalVolume = _environmentMapper.volume;
//        var globalVolumeDims = new Vector3Int(globalVolume.width, globalVolume.height, globalVolume.volumeDepth);

//        int startX = (globalVolumeDims.x / 2) - (_chunkDimensions.x / 2);
//        int startZ = (globalVolumeDims.z / 2) - (_chunkDimensions.z / 2);

//        for (int y = 0; y < 4; y++)
//        {
//            Vector3Int chunkVoxelOrigin = new Vector3Int(startX, y * 32, startZ);
//            GameObject chunkGO = await CreateAndMeshChunk(chunkVoxelOrigin);
//            if (chunkGO != null)
//            {
//                _chunkGameObjects.Add(chunkGO);
//            }
//        }

//        Debug.Log("<color=cyan>--- Multi-Chunk Test Complete ---</color>");
//    }

//    private async Task<GameObject> CreateAndMeshChunk(Vector3Int voxelOrigin)
//    {
//        var mesher = new Mesher();
//        NativeArray<sbyte> voxelData = default;
//        Chunk mesherChunk = null;
//        GameObject chunkGameObject = null;

//        try
//        {
//            voxelData = await _voxelProvider.GetVoxelDataForChunk(voxelOrigin, _chunkDimensions);
//            if (!voxelData.IsCreated || voxelData.Length == 0) return null;

//            mesherChunk = new Chunk();
//            voxelData.CopyTo(mesherChunk.data);

//            mesher.StartMeshJob(mesherChunk, Mesher.NormalCalculationMode.FromSDF);

//            while (!mesher.IsFinished()) { await Task.Yield(); }
//            mesher.WaitForMeshJob();

//            if (mesher.Vertices.Length > 0)
//            {
//                // --- The new, HIGH-PERFORMANCE workflow starts here ---

//                // 1. Prepare the Job struct with our data.
//                // We create new NativeArrays to hold the output. Use TempJob Allocator
//                // so they are automatically cleaned up after a few frames.
//                var scaleJob = new ScaleVerticesJob
//                {
//                    RawVertices = mesher.Vertices,
//                    MetersPerVoxel = _environmentMapper.metersPerVoxel,
//                    FinalVertices = new NativeArray<float3>(mesher.Vertices.Length, Allocator.TempJob),
//                    FinalNormals = new NativeArray<float3>(mesher.Vertices.Length, Allocator.TempJob)
//                };

//                // 2. Schedule the Job to run on the background worker threads, and wait for it.
//                // The `Schedule(length, batch_count)` tells the system to split the work up efficiently.
//                JobHandle handle = scaleJob.Schedule(mesher.Vertices.Length, 64);

//                // This is a blocking call, but it's very fast. For a real chunk manager,
//                // we would pass the handle out and wait on it later. For this test, Complete() is fine.
//                handle.Complete();

//                // At this point, scaleJob.FinalVertices and scaleJob.FinalNormals are filled.

//                // --- The rest of the code is now simpler ---

//                chunkGameObject = new GameObject($"Chunk_{voxelOrigin.x}_{voxelOrigin.y}_{voxelOrigin.z}");
//                float metersPerVoxel = _environmentMapper.metersPerVoxel;
//                var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
//                //if (_worldAnchor != null)
//                //{
//                //    chunkGameObject.transform.SetParent(_worldAnchor, worldPositionStays: false);
//                ////}
//                //float metersPerVoxel = _environmentMapper.metersPerVoxel;
//                //var globalVolumeCenterOffsetInVoxels = new Vector3(_environmentMapper.volume.width / 2f, _environmentMapper.volume.height / 2f, _environmentMapper.volume.volumeDepth / 2f);
//                //var chunkOriginInVoxels = new Vector3(voxelOrigin.x, voxelOrigin.y, voxelOrigin.z);
//                //// We use localPosition because it's now a child of the anchor.
//                ////chunkGameObject.transform.localPosition = (chunkOriginInVoxels - globalVolumeCenterOffsetInVoxels) * metersPerVoxel;
//                //float metersPerVoxel = _environmentMapper.metersPerVoxel;
//                //var globalVolumeCenterOffsetInVoxels = new Vector3(_environmentMapper.volume.width / 2f, _environmentMapper.volume.height / 2f, _environmentMapper.volume.volumeDepth / 2f);
//                //var chunkOriginInVoxels = new Vector3(voxelOrigin.x, voxelOrigin.y, voxelOrigin.z);

//                // The crucial change: set the absolute .position, not the relative .localPosition
//                //chunkGameObject.transform.position = (chunkOriginInVoxels - globalVolumeCenterOffsetInVoxels) * metersPerVoxel;
//                Vector3 chunkWorldPos = ((Vector3)voxelOrigin - (globalVolumeDims / 2.0f)) * metersPerVoxel;

//                chunkGameObject.transform.position = chunkWorldPos;
//                // ... (GameObject positioning is the same) ...
//                var meshFilter = chunkGameObject.AddComponent<MeshFilter>();
//                var meshRenderer = chunkGameObject.AddComponent<MeshRenderer>();
//                var meshCollider = chunkGameObject.AddComponent<MeshCollider>();
//                meshRenderer.material = _meshMaterial;

//                var newMesh = new Mesh();
//                // Note: mesher.Vertices and scaleJob.FinalVertices have the same length
//                if (mesher.Vertices.Length > 65534) newMesh.indexFormat = IndexFormat.UInt32;

//                // Apply the data directly from our job's output arrays.
//                // The Mesh class can take NativeArrays directly, which is very fast!
//                newMesh.SetVertices(scaleJob.FinalVertices);
//                newMesh.SetNormals(scaleJob.FinalNormals);
//                newMesh.SetIndices(mesher.Indices, MeshTopology.Triangles, 0); // Get indices from original mesher
//                newMesh.RecalculateBounds();

//                meshFilter.mesh = newMesh;
//                meshCollider.sharedMesh = newMesh;

//                // --- CRITICAL: Dispose the temporary NativeArrays! ---
//                scaleJob.FinalVertices.Dispose();
//                scaleJob.FinalNormals.Dispose();

//                Debug.Log($"<color=green>SUCCESS (Optimized):</color> Created chunk at {voxelOrigin} with {mesher.Vertices.Length} vertices.");
//            }
//        }
//        finally
//        {
//            mesher?.Dispose();
//            mesherChunk?.Dispose();
//            if (voxelData.IsCreated) voxelData.Dispose();
//        }

//        return chunkGameObject;
//    }
//}