using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Anaglyph.XRTemplate;

public class ChunkManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private EnvironmentMapper _environmentMapper;
    [SerializeField] private VoxelProvider _voxelProvider;
    [SerializeField] private Material _meshMaterial;

    [Header("Core References")]
    [SerializeField] private OVRCameraRig _cameraRig;
    [SerializeField] private Transform _playerHeadTransform;

    [Header("Generation Settings")]
    [SerializeField] private int _chunkGenerationRadius = 2;
    [SerializeField] private float _updateInterval = 1.0f; // 1 Hz
    [SerializeField] private float _chunkRemeshInterval = 5.0f; // Chunks refresh every 5 seconds

    // --- CHANGE 1: The dictionary now tracks the instance and its creation/update time ---
    private readonly Dictionary<Vector3Int, (ChunkInstance instance, float lastUpdateTime)> _activeChunks = new();
    private readonly Vector3Int _chunkDimensions = new(MarchingCubes.Chunk.ChunkSizeX, MarchingCubes.Chunk.ChunkSizeY, MarchingCubes.Chunk.ChunkSizeZ);

    void Start()
    {
        // ... (Start method is the same) ...
        if (_environmentMapper == null || _voxelProvider == null || _cameraRig == null || _playerHeadTransform == null)
        {
            Debug.LogError("FATAL: A dependency has not been assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        StartCoroutine(UpdateChunksCoroutine());
    }

    private IEnumerator UpdateChunksCoroutine()
    {
        while (true)
        {
            var requiredChunks = new HashSet<Vector3Int>();
            Vector3Int playerChunkCoord = WorldPosToChunkCoord(_playerHeadTransform.position);

            for (int x = -_chunkGenerationRadius; x <= _chunkGenerationRadius; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -_chunkGenerationRadius; z <= _chunkGenerationRadius; z++)
                    {
                        requiredChunks.Add(playerChunkCoord + new Vector3Int(x, y, z));
                    }
                }
            }

            var chunksToUnload = new List<Vector3Int>();
            foreach (var chunkCoord in _activeChunks.Keys)
            {
                if (!requiredChunks.Contains(chunkCoord))
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }

            foreach (var coord in chunksToUnload)
            {
                if (_activeChunks.TryGetValue(coord, out var chunkData) && chunkData.instance != null)
                {
                    Destroy(chunkData.instance.gameObject);
                }
                _activeChunks.Remove(coord);
            }

            foreach (var coord in requiredChunks)
            {
                if (!_activeChunks.ContainsKey(coord))
                {
                    // --- This is for loading NEW chunks ---
                    GameObject newChunkObject = new GameObject();
                    ChunkInstance newInstance = newChunkObject.AddComponent<ChunkInstance>();

                    newInstance.Initialize(coord, OnChunkBuildFailed, _voxelProvider, _meshMaterial, _environmentMapper, _cameraRig);

                    _activeChunks.Add(coord, (newInstance, Time.time));
                }
                else
                {
                    // --- CHANGE 2: This is for UPDATING EXISTING chunks ---
                    var chunkData = _activeChunks[coord];
                    if (Time.time - chunkData.lastUpdateTime > _chunkRemeshInterval)
                    {
                        // It's time to refresh!
                        chunkData.instance.TriggerUpdate();
                        // Update the timestamp so we don't try to update it again next frame
                        _activeChunks[coord] = (chunkData.instance, Time.time);
                    }
                }
            }

            yield return new WaitForSeconds(_updateInterval);
        }
    }

    private void OnChunkBuildFailed(Vector3Int coordinate)
    {
        _activeChunks.Remove(coordinate);
    }

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
}