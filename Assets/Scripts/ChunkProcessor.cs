using UnityEngine;
using System.Threading.Tasks;
using MarchingCubes;
using System.Linq;
using Anaglyph.XRTemplate;

// The Worker in our architecture. Its only job is to process tasks from the queues.
public class ChunkProcessor : MonoBehaviour
{
    [Header("Dependencies (from Manager)")]
    [SerializeField] private ChunkManagerV2 _chunkManager;
    [SerializeField] private VoxelProvider _voxelProvider;
    [SerializeField] private Material _meshMaterial;
    [SerializeField] private EnvironmentMapper _environmentMapper;
    [SerializeField] private OVRCameraRig _cameraRig;
    [SerializeField] private GameObject _chunkPrefab; // A prefab for creating new chunk instances.

    private bool _isBusy = false;

    void Update()
    {
        // If we are not already busy with a task, check for new work.
        if (_isBusy) return;

        // Process queues in order of priority.
        if (_chunkManager.ProcessCreationQueue(out var createCoord))
        {
            // We got a coordinate to create. Start the async task.
            ProcessChunkTask(createCoord, true);
        }
        else if (_chunkManager.ProcessRemeshQueue(out var remeshCoord))
        {
            // We got a coordinate to remesh.
            ProcessChunkTask(remeshCoord, false);
        }
        else if (_chunkManager.ProcessDestructionQueue(out var destroyCoord))
        {
            // Destruction is fast and synchronous.
            if (_chunkManager.ActiveChunks.TryGetValue(destroyCoord, out ChunkState state) && state.instance != null)
            {
                Destroy(state.instance.gameObject);
                _chunkManager.ActiveChunks.Remove(destroyCoord);
            }
        }
    }

    private async void ProcessChunkTask(Vector3Int coord, bool isInitialBuild)
    {
        _isBusy = true;
        _chunkManager.SetChunkStatus(coord, isInitialBuild ? ChunkStatus.IsBeingCreated : ChunkStatus.IsBeingRemeshed);

        try
        {
            // --- 1. Build the Mesh (Async) ---
            // This is the heavy lifting, done without blocking the main thread.
            Mesh newMesh = await BuildMeshTask(coord);

            // --- 2. Apply the Result (Back on Main Thread) ---
            // The await completes, and the rest of this code runs on the main thread.
            if (newMesh != null)
            {
                if (isInitialBuild)
                {
                    // If it's a new chunk, we need to instantiate it.
                    GameObject newChunkObject = Instantiate(_chunkPrefab, transform);
                    ChunkInstanceV2 newInstance = newChunkObject.GetComponent<ChunkInstanceV2>();
                    newInstance.Initialize(coord, _meshMaterial); // A much simpler Initialize.
                    _chunkManager.LinkChunkInstance(coord, newInstance);
                    newInstance.ApplyMesh(newMesh);
                }
                else
                {
                    // If it's a remesh, the instance already exists.
                    if (_chunkManager.ActiveChunks.TryGetValue(coord, out ChunkState state) && state.instance != null)
                    {
                        state.instance.ApplyMesh(newMesh);
                    }
                }
                _chunkManager.SetChunkStatus(coord, ChunkStatus.Idle);
                _chunkManager.UpdateChunkTimestamp(coord);
            }
            else
            {
                // The build failed (e.g., empty chunk).
                _chunkManager.HandleBuildFailed(coord);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"An error occurred while processing chunk {coord}.");
            Debug.LogException(e);
            _chunkManager.HandleBuildFailed(coord);
        }
        finally
        {
            _isBusy = false;
        }
    }

    // This is the CORE meshing logic, extracted from your old ChunkInstance.
    // It is now a generic function that can be called by the processor.
    private async Task<Mesh> BuildMeshTask(Vector3Int coordinate)
    {
        var mesher = new Mesher();
        Chunk chunkData = null;
        Mesh finalMesh = null;
        try
        {
            var voxelOrigin = ChunkCoordToVoxelOrigin(coordinate);
            chunkData = await _voxelProvider.GetVoxelDataForChunk(voxelOrigin, new Vector3Int(Chunk.ChunkSizeX, Chunk.ChunkSizeY, Chunk.ChunkSizeZ));

            if (chunkData == null) return null;

            mesher.StartMeshJob(chunkData, Mesher.Mode.Simd32Multithreaded);
            mesher.WaitForMeshJob(); // This is still blocking, but it's inside an async task, so it only blocks the background thread, not the game.

            if (mesher.Vertices.Length > 2)
            {
                Vector3 unrotatedPosition = ChunkVoxelOriginToLocalPos(voxelOrigin);
                transform.position = _cameraRig.trackingSpace.TransformPoint(unrotatedPosition);
                transform.rotation = _cameraRig.trackingSpace.rotation * Quaternion.Euler(0, -90, 0);
                float scale = _environmentMapper.metersPerVoxel;
                float gapCorrectionScale = (float)Chunk.ChunkSizeX / (Chunk.ChunkSizeX - 1.0f);
                transform.localScale = new Vector3(scale, scale, -scale) * gapCorrectionScale;

                finalMesh = new Mesh();
                finalMesh.SetMesh(mesher);
                finalMesh.triangles = finalMesh.triangles.Reverse().ToArray();
                finalMesh.RecalculateNormals();
                finalMesh.RecalculateBounds();
            }
            else
            {
                return null; // Empty chunk, not an error.
            }
        }
        finally
        {
            mesher?.Dispose();
            chunkData?.Dispose();
        }
        return finalMesh;
    }

    // --- Helper functions also moved from ChunkInstance ---
    private Vector3Int ChunkCoordToVoxelOrigin(Vector3Int chunkCoord)
    {
        var dims = new Vector3Int(Chunk.ChunkSizeX, Chunk.ChunkSizeY, Chunk.ChunkSizeZ);
        return new Vector3Int(chunkCoord.x * dims.x, chunkCoord.y * dims.y, chunkCoord.z * dims.z);
    }

    private Vector3 ChunkVoxelOriginToLocalPos(Vector3Int voxelOrigin)
    {
        var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        return ((Vector3)voxelOrigin - (globalVolumeDims / 2.0f)) * _environmentMapper.metersPerVoxel;
    }
}