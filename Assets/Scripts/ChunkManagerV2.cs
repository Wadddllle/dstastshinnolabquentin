using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Anaglyph.XRTemplate;

// The Coordinator in our architecture. Its only job is to decide what work needs to be done
// and place it into queues for the Processor. It does NO heavy lifting.
public class ChunkManagerV2 : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnvironmentMapper _environmentMapper;
    [SerializeField] private OVRCameraRig _cameraRig;
    [SerializeField] private Transform _playerHeadTransform;

    [Header("Coordination Settings")]
    [SerializeField] private int _chunkGenerationRadius = 2;
    [SerializeField] private float _chunkDiscoveryInterval = 0.5f; // How often we check for new/old chunks.
    [SerializeField] private float _chunkRemeshInterval = 5.0f;

    // --- State Management & Queues ---
    public readonly Dictionary<Vector3Int, ChunkState> ActiveChunks = new();
    private readonly Queue<Vector3Int> _chunksToCreate = new();
    private readonly Queue<Vector3Int> _chunksToRemesh = new();
    private readonly Queue<Vector3Int> _chunksToDestroy = new();

    private readonly Vector3Int _chunkDimensions = new(MarchingCubes.Chunk.ChunkSizeX, MarchingCubes.Chunk.ChunkSizeY, MarchingCubes.Chunk.ChunkSizeZ);
    private bool _areChunksVisible = true;

    void Start()
    {
        if (_environmentMapper == null || _cameraRig == null || _playerHeadTransform == null)
        {
            Debug.LogError("FATAL: A dependency has not been assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        StartCoroutine(CoordinateChunksCoroutine());
    }

    private IEnumerator CoordinateChunksCoroutine()
    {
        while (true)
        {
            // --- Phase 1: Discovery & Unloading ---
            var requiredCoords = GetRequiredChunkCoords();

            // Find chunks that are needed but don't exist yet.
            foreach (var coord in requiredCoords)
            {
                if (!ActiveChunks.ContainsKey(coord))
                {
                    // Add to dictionary and queue for creation.
                    ActiveChunks.Add(coord, new ChunkState(coord));
                    _chunksToCreate.Enqueue(coord);
                }
            }

            // Find chunks that exist but are no longer needed.
            var coordsToDestroy = new List<Vector3Int>();
            foreach (var chunk in ActiveChunks)
            {
                if (!requiredCoords.Contains(chunk.Key) && chunk.Value.status != ChunkStatus.QueuedForDestruction)
                {
                    chunk.Value.status = ChunkStatus.QueuedForDestruction;
                    _chunksToDestroy.Enqueue(chunk.Key);
                }
            }

            // --- Phase 2: Schedule Remeshing ---
            foreach (var chunk in ActiveChunks.Values)
            {
                // Only idle chunks can be scheduled for a remesh.
                if (chunk.status == ChunkStatus.Idle && Time.time - chunk.lastUpdateTime > _chunkRemeshInterval)
                {
                    chunk.status = ChunkStatus.QueuedForRemesh;
                    _chunksToRemesh.Enqueue(chunk.coordinate);
                }
            }

            yield return new WaitForSeconds(_chunkDiscoveryInterval);
        }
    }

    private HashSet<Vector3Int> GetRequiredChunkCoords()
    {
        var requiredChunks = new HashSet<Vector3Int>();
        Vector3Int playerChunkCoord = WorldPosToChunkCoord(_playerHeadTransform.position);

        for (int x = -_chunkGenerationRadius; x <= _chunkGenerationRadius; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -_chunkGenerationRadius; z <= _chunkGenerationRadius; z++)
                {
                    requiredChunks.Add(playerChunkCoord + new Vector3Int(x, y, z));
                }
        return requiredChunks;
    }

    // --- Public methods for the Processor to call ---

    public bool ProcessCreationQueue(out Vector3Int coordinate) => _chunksToCreate.TryDequeue(out coordinate);
    public bool ProcessRemeshQueue(out Vector3Int coordinate) => _chunksToRemesh.TryDequeue(out coordinate);
    public bool ProcessDestructionQueue(out Vector3Int coordinate) => _chunksToDestroy.TryDequeue(out coordinate);

    public void SetChunkStatus(Vector3Int coord, ChunkStatus status)
    {
        if (ActiveChunks.TryGetValue(coord, out ChunkState state)) state.status = status;
    }

    public void UpdateChunkTimestamp(Vector3Int coord)
    {
        if (ActiveChunks.TryGetValue(coord, out ChunkState state)) state.lastUpdateTime = Time.time;
    }

    public void LinkChunkInstance(Vector3Int coord, ChunkInstanceV2 instance)
    {
        if (ActiveChunks.TryGetValue(coord, out ChunkState state)) state.instance = instance;
    }

    public void HandleBuildFailed(Vector3Int coord)
    {
        // If a build fails, just remove it. It will be re-queued for creation on the next discovery cycle if it's still needed.
        if (ActiveChunks.TryGetValue(coord, out ChunkState state) && state.instance != null)
        {
            Destroy(state.instance.gameObject);
        }
        ActiveChunks.Remove(coord);
    }

    // ... (WorldPosToChunkCoord and ToggleChunkVisibility can remain mostly the same) ...
    private Vector3Int WorldPosToChunkCoord(Vector3 worldPos)
    {
        Vector3 localPos = _cameraRig.trackingSpace.InverseTransformPoint(worldPos);
        var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        Vector3 voxelPos = (localPos / _environmentMapper.metersPerVoxel) + (globalVolumeDims / 2.0f);
        return new Vector3Int(
            Mathf.FloorToInt(voxelPos.x / _chunkDimensions.x),
            Mathf.FloorToInt(voxelPos.y / _chunkDimensions.y),
            Mathf.FloorToInt(voxelPos.z / _chunkDimensions.z)
        );
    }

    public void ToggleChunkVisibility()
    {
        _areChunksVisible = !_areChunksVisible;
        foreach (var chunkState in ActiveChunks.Values)
        {
            if (chunkState.instance != null)
            {
                chunkState.instance.SetRendererVisibility(_areChunksVisible);
            }
        }
    }
}