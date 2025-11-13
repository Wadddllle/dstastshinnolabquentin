using UnityEngine;
using System.Threading.Tasks;
using MarchingCubes;
using Unity.Collections;
using System;
using System.Linq; // Needed for the triangle flipping
using Anaglyph.XRTemplate;

public class MarchingCubesChunkManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnvironmentMapper _environmentMapper;
    [SerializeField] private VoxelProvider _voxelProvider;
    [SerializeField] private Material _meshMaterial;

    [Header("Core Reference for Alignment")]
    [SerializeField] private OVRCameraRig _cameraRig;

    private readonly Vector3Int _chunkDimensions = new(Chunk.ChunkSizeX, Chunk.ChunkSizeY, Chunk.ChunkSizeZ);

    void Start()
    {
        if (_environmentMapper == null || _voxelProvider == null || _cameraRig == null)
        {
            Debug.LogError("FATAL: A dependency has not been assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        var globalVolumeDims = new Vector3Int(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        Vector3Int testChunkCoord = new Vector3Int(
            (globalVolumeDims.x / _chunkDimensions.x) / 2,
            (globalVolumeDims.y / _chunkDimensions.y) / 2,
            (globalVolumeDims.z / _chunkDimensions.z) / 2
        );
        Debug.Log($"<color=yellow>--- FALLBACK TEST (Handedness-Corrected) ---</color>");
        Debug.Log($"Attempting to generate a single chunk at coordinate: {testChunkCoord}");
        CreateAndMeshChunk(testChunkCoord);
    }

    private async void CreateAndMeshChunk(Vector3Int chunkCoord)
    {
        try
        {
            await BuildChunkMesh(chunkCoord);
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while creating chunk {chunkCoord}.");
            Debug.LogException(e);
        }
    }

    private async Task<GameObject> BuildChunkMesh(Vector3Int chunkCoord)
    {
        var mesher = new Mesher();
        Chunk testChunk = null;
        GameObject chunkGameObject = null;

        try
        {
            var voxelOrigin = ChunkCoordToVoxelOrigin(chunkCoord);
            testChunk = await _voxelProvider.GetVoxelDataForChunk(voxelOrigin, _chunkDimensions);
            if (testChunk == null) return null;

            mesher.StartMeshJob(testChunk, Mesher.Mode.Simd32);
            mesher.WaitForMeshJob();

            if (mesher.Vertices.Length > 2)
            {
                chunkGameObject = new GameObject($"TestChunk_{chunkCoord.x}_{chunkCoord.y}_{chunkCoord.z}");

                // --- APPLYING YOUR EXACT TRANSFORMATIONS ---
                Vector3 unrotatedPosition = ChunkVoxelOriginToWorldPos(voxelOrigin);
                chunkGameObject.transform.position = _cameraRig.trackingSpace.TransformPoint(unrotatedPosition);

                // 1. Apply the base rotation of the playspace
                // 2. Apply your discovered -90 degree fix on the Y axis
                chunkGameObject.transform.rotation = _cameraRig.trackingSpace.rotation * Quaternion.Euler(0, -90, 0);

                // 3. Apply the handedness-flipping negative scale
                float scale = _environmentMapper.metersPerVoxel;
                chunkGameObject.transform.localScale = new Vector3(scale, scale, -scale);

                var meshFilter = chunkGameObject.AddComponent<MeshFilter>();
                var meshRenderer = chunkGameObject.AddComponent<MeshRenderer>();
                var meshCollider = chunkGameObject.AddComponent<MeshCollider>();
                meshRenderer.material = _meshMaterial;

                var newMesh = new Mesh();
                newMesh.SetMesh(mesher); // Upload the raw data

                // --- CRITICAL FIX FOR NEGATIVE SCALE ---
                // We must flip the triangles to correct the winding order
                newMesh.triangles = newMesh.triangles.Reverse().ToArray();
                // --- END FIX ---

                newMesh.RecalculateNormals(); // Now normals will point outwards correctly
                newMesh.RecalculateBounds();

                meshFilter.mesh = newMesh;
                meshCollider.sharedMesh = newMesh;
                Debug.Log($"<color=green>SUCCESS: Test chunk generated with {mesher.Vertices.Length} vertices.</color>");
            }
            else
            {
                Debug.LogWarning($"Test chunk generated with 0 vertices.");
            }
        }
        finally
        {
            mesher?.Dispose();
            testChunk?.Dispose();
        }
        return chunkGameObject;
    }

    // --- Coordinate Functions ---
    private Vector3Int ChunkCoordToVoxelOrigin(Vector3Int chunkCoord)
    {
        return new Vector3Int(
            chunkCoord.x * _chunkDimensions.x,
            chunkCoord.y * _chunkDimensions.y,
            chunkCoord.z * _chunkDimensions.z
        );
    }
    private Vector3 ChunkVoxelOriginToWorldPos(Vector3Int voxelOrigin)
    {
        var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        return ((Vector3)voxelOrigin - (globalVolumeDims / 2.0f)) * _environmentMapper.metersPerVoxel;
    }
}