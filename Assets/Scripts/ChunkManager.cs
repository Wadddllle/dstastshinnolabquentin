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
    // --- ADD THIS ---
    [SerializeField] private Material _occlusionMaterial; // Drag your 'Mat_Occlusion' here
    private bool _isOcclusionMode = false;

    [Header("Core References")]
    [SerializeField] private OVRCameraRig _cameraRig;
    [SerializeField] private Transform _playerHeadTransform;

    [Header("Generation Settings")]
    [SerializeField] private int _chunkGenerationRadius = 2;
    [SerializeField] private float _chunkDiscoveryInterval = 1.0f; // Formerly _updateInterval
    [SerializeField] private float _chunkRemeshInterval = 2.0f;
    [SerializeField] private float _remeshCheckInterval = 0.25f; // How often we check if we NEED to remesh
    [SerializeField] private string _envLayerName = "Environment";

    private int _envLayerId;

    private readonly Vector3Int _chunkStride = new Vector3Int(31, 31, 31);
    private readonly Dictionary<Vector3Int, (ChunkInstance instance, float lastUpdateTime)> _activeChunks = new();
    private readonly Vector3Int _chunkDimensions = new(MarchingCubes.Chunk.ChunkSizeX, MarchingCubes.Chunk.ChunkSizeY, MarchingCubes.Chunk.ChunkSizeZ);

    private bool _areChunksVisible = true;

    void Start()
    {
        _envLayerId = LayerMask.NameToLayer(_envLayerName);

        if (_envLayerId == -1)
        {
            Debug.LogError($"Layer '{_envLayerName}' does not exist! Please add it in Unity's Layer settings.");
            _envLayerId = 0;
        }

        if (_environmentMapper == null || _voxelProvider == null || _cameraRig == null || _playerHeadTransform == null)
        {
            Debug.LogError("FATAL: A dependency has not been assigned in the Inspector!", this);
            enabled = false;
            return;
        }

        StartCoroutine(UpdateChunkGenerationCoroutine());
        StartCoroutine(UpdateChunkRemeshingCoroutine());
    }

    public void ToggleChunkVisibility()
    {
        ToggleChunkVisibility(!_areChunksVisible);
    }

    public void ToggleChunkVisibility(bool isVisible)
    {
        _areChunksVisible = isVisible;

        foreach (var chunkData in _activeChunks.Values)
        {
            if (chunkData.instance != null)
            {
                chunkData.instance.SetRendererVisibility(isVisible);
            }
        }
        Debug.Log($"Chunk renderers set to: {(isVisible ? "Visible" : "Hidden")}");
    }

    public void SetOcclusionMode(bool enableOcclusion)
    {
        _isOcclusionMode = enableOcclusion;
        Material targetMat = _isOcclusionMode ? _occlusionMaterial : _meshMaterial;

        // 1. Swap material on all currently active chunks
        foreach (var chunkData in _activeChunks.Values)
        {
            if (chunkData.instance != null)
            {
                // Ensure renderer is enabled so Z-write happens
                chunkData.instance.SetRendererVisibility(true);

                // Swap the material
                var rend = chunkData.instance.GetComponent<Renderer>();
                if (rend != null) rend.material = targetMat;
            }
        }

        // 2. Ensure future chunks know which visibility state to use
        _areChunksVisible = true; // Always true in occlusion mode (it's just invisible paint)
        Debug.Log($"Occlusion Mode set to: {enableOcclusion}");
    }

    /// <summary>
    /// This coroutine ONLY handles creating new chunks.
    /// UNLOADING HAS BEEN DISABLED so the MapGenerator can capture the full area.
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

            // =========================================================
            //  UNLOADING LOGIC COMMENTED OUT FOR MAP GENERATION
            // =========================================================
            /*
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
            */
            // =========================================================

            foreach (var coord in requiredChunks)
            {
                if (!_activeChunks.ContainsKey(coord))
                {
                    GameObject newChunkObject = new GameObject();
                    ChunkInstance newInstance = newChunkObject.AddComponent<ChunkInstance>();
                    //newInstance.Initialize(coord, OnChunkBuildFailed, _voxelProvider, _meshMaterial, _environmentMapper, _cameraRig, _areChunksVisible, _floorLayerId);
                    Material matToUse = _isOcclusionMode ? _occlusionMaterial : _meshMaterial;
                    newInstance.Initialize(coord, OnChunkBuildFailed, _voxelProvider, matToUse, _environmentMapper, _cameraRig, _areChunksVisible, _envLayerId);
                    _activeChunks.Add(coord, (newInstance, Time.time));
                }
            }

            yield return new WaitForSeconds(_chunkDiscoveryInterval);
        }
    }

    private IEnumerator UpdateChunkRemeshingCoroutine()
    {
        while (true)
        {
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
                if (_activeChunks.TryGetValue(coord, out var chunkData))
                {
                    chunkData.instance.TriggerUpdate();
                    _activeChunks[coord] = (chunkData.instance, Time.time);
                }
            }

            yield return new WaitForSeconds(_remeshCheckInterval);
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
            Mathf.FloorToInt(voxelPos.x / _chunkStride.x),
            Mathf.FloorToInt(voxelPos.y / _chunkStride.y),
            Mathf.FloorToInt(voxelPos.z / _chunkStride.z)
        );
    }

    public Dictionary<Vector3Int, (ChunkInstance instance, float lastUpdateTime)> GetActiveChunks()
    {
        // Return a copy to prevent external scripts from accidentally modifying it
        return new Dictionary<Vector3Int, (ChunkInstance instance, float lastUpdateTime)>(_activeChunks);
    }

}