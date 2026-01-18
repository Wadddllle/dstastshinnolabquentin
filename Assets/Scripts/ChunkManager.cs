
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
    [SerializeField] private float _chunkDiscoveryInterval = 1.0f; // Formerly _updateInterval
    [SerializeField] private float _chunkRemeshInterval = 2.0f;
    [SerializeField] private float _remeshCheckInterval = 0.25f; // How often we check if we NEED to remesh
    [SerializeField] private string _floorLayerName = "Floor"; // NEW: Type the name of your layer here

    private int _floorLayerId; // We will store the ID here


    private readonly Vector3Int _chunkStride = new Vector3Int(31, 31, 31);

    private readonly Dictionary<Vector3Int, (ChunkInstance instance, float lastUpdateTime)> _activeChunks = new();
    private readonly Vector3Int _chunkDimensions = new(MarchingCubes.Chunk.ChunkSizeX, MarchingCubes.Chunk.ChunkSizeY, MarchingCubes.Chunk.ChunkSizeZ);

    private bool _areChunksVisible = true;
    void Start()
    {
        // 1.FIND THE LAYER ID
        _floorLayerId = LayerMask.NameToLayer(_floorLayerName);

        // Safety Check
        if (_floorLayerId == -1)
        {
            Debug.LogError($"Layer '{_floorLayerName}' does not exist! Please add it in Unity's Layer settings.");
            _floorLayerId = 0; // Default (fallback)
        }

        if (_environmentMapper == null || _voxelProvider == null || _cameraRig == null || _playerHeadTransform == null)
        {
            Debug.LogError("FATAL: A dependency has not been assigned in the Inspector!", this);
            enabled = false;
            return;
        }
        // Start BOTH coroutines
        StartCoroutine(UpdateChunkGenerationCoroutine());
        StartCoroutine(UpdateChunkRemeshingCoroutine());
    }
    public void ToggleChunkVisibility()
    {
        _areChunksVisible = !_areChunksVisible;
        foreach (var chunkData in _activeChunks.Values)
        {
            if (chunkData.instance != null)
            {
                // MODIFIED: Call the new method on the instance
                chunkData.instance.SetRendererVisibility(_areChunksVisible);
            }
        }
        Debug.Log($"Chunk renderers toggled to: {(_areChunksVisible ? "Visible" : "Hidden")}");
    }
    // ... (ToggleChunkVisibility remains the same) ...

    /// <summary>
    /// This coroutine ONLY handles creating new chunks and unloading old ones.
    /// It can run slower because player position doesn't need to be checked constantly.
    /// </summary>
    private IEnumerator UpdateChunkGenerationCoroutine()
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
                    GameObject newChunkObject = new GameObject();
                    ChunkInstance newInstance = newChunkObject.AddComponent<ChunkInstance>();
                    newInstance.Initialize(coord, OnChunkBuildFailed, _voxelProvider, _meshMaterial, _environmentMapper, _cameraRig, _areChunksVisible, _floorLayerId);
                    _activeChunks.Add(coord, (newInstance, Time.time));
                }
            }

            yield return new WaitForSeconds(_chunkDiscoveryInterval);
        }
    }

    /// <summary>
    /// This coroutine ONLY handles remeshing existing chunks.
    /// It runs much faster to ensure the remesh interval is respected.
    /// </summary>
    private IEnumerator UpdateChunkRemeshingCoroutine()
    {
        while (true)
        {
            // Create a temporary list to avoid modifying the dictionary while iterating
            var chunksToUpdate = new List<Vector3Int>();
            foreach (var chunk in _activeChunks)
            {
                var coord = chunk.Key;
                var chunkData = chunk.Value;

                if (Time.time - chunkData.lastUpdateTime > _chunkRemeshInterval)
                {
                    chunksToUpdate.Add(coord);
                }
            }

            foreach (var coord in chunksToUpdate)
            {
                // We still need to check if the chunk exists, as it might have been
                // unloaded by the other coroutine between the loops.
                if (_activeChunks.TryGetValue(coord, out var chunkData))
                {
                    chunkData.instance.TriggerUpdate();
                    _activeChunks[coord] = (chunkData.instance, Time.time);
                }
            }

            // This loop runs frequently to check the time condition accurately.
            yield return new WaitForSeconds(_remeshCheckInterval);
        }
    }


    private void OnChunkBuildFailed(Vector3Int coordinate)
    {
        _activeChunks.Remove(coordinate);
    }

    private Vector3Int WorldPosToChunkCoord(Vector3 worldPos)
    {
        //Vector3 localPos = _cameraRig.trackingSpace.InverseTransformPoint(worldPos);
        //var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        //Vector3 voxelPos = (localPos / _environmentMapper.metersPerVoxel) + (globalVolumeDims / 2.0f);
        //return new Vector3Int(
        //    Mathf.FloorToInt(voxelPos.x / _chunkDimensions.x),
        //    Mathf.FloorToInt(voxelPos.y / _chunkDimensions.y),
        //    Mathf.FloorToInt(voxelPos.z / _chunkDimensions.z)
        //);
        Vector3 localPos = _cameraRig.trackingSpace.InverseTransformPoint(worldPos);
        var globalVolumeDims = new Vector3(_environmentMapper.volume.width, _environmentMapper.volume.height, _environmentMapper.volume.volumeDepth);
        Vector3 voxelPos = (localPos / _environmentMapper.metersPerVoxel) + (globalVolumeDims / 2.0f);

        return new Vector3Int(
            Mathf.FloorToInt(voxelPos.x / _chunkStride.x),
            Mathf.FloorToInt(voxelPos.y / _chunkStride.y),
            Mathf.FloorToInt(voxelPos.z / _chunkStride.z)
        );
    }
}