using UnityEngine;
using System.Threading.Tasks;
using MarchingCubes;
using Unity.Collections;
using System;
using System.Linq;
using Anaglyph.XRTemplate;
using Unity.Jobs;

public class ChunkInstance : MonoBehaviour
{
    // --- State & Dependencies ---
    private Vector3Int _coordinate;
    private Action<Vector3Int> _onBuildFailed;
    private VoxelProvider _voxelProvider;
    private Material _meshMaterial;
    private EnvironmentMapper _environmentMapper;
    private OVRCameraRig _cameraRig;
    private bool _isUpdating = false;
    private bool _isRendererVisible = true; // Tracks the desired state of the renderer

    // --- Components ---
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private MeshRenderer _meshRenderer; // Reference to the renderer component

    // MODIFIED: Added 'isVisible' parameter
    public void Initialize(Vector3Int coordinate, Action<Vector3Int> onBuildFailed, VoxelProvider provider, Material material, EnvironmentMapper mapper, OVRCameraRig cameraRig, bool isVisible)
    {
        _coordinate = coordinate;
        _onBuildFailed = onBuildFailed;
        _voxelProvider = provider;
        _meshMaterial = material;
        _environmentMapper = mapper;
        _cameraRig = cameraRig;
        _isRendererVisible = isVisible; // Store the initial visibility state

        gameObject.name = $"Chunk_{_coordinate.x}_{_coordinate.y}_{_coordinate.z}";
        InitialBuild();
    }

    /// <summary>
    /// Public method to control the visibility of the chunk's mesh renderer.
    /// </summary>
    public void SetRendererVisibility(bool isVisible)
    {
        _isRendererVisible = isVisible;
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = _isRendererVisible;
        }
    }

    public void TriggerUpdate()
    {
        if (_isUpdating) return;
        UpdateMesh();
    }

    private async void InitialBuild()
    {
        try
        {
            Mesh newMesh = await BuildMeshTask();
            if (newMesh != null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
                _meshRenderer = gameObject.AddComponent<MeshRenderer>(); // Store the reference
                _meshRenderer.material = _meshMaterial;
                _meshRenderer.enabled = _isRendererVisible; // Set initial state
                _meshCollider = gameObject.AddComponent<MeshCollider>();

                _meshFilter.sharedMesh = newMesh;
                _meshCollider.sharedMesh = newMesh;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred during initial build of chunk {_coordinate}.");
            Debug.LogException(e);
            _onBuildFailed?.Invoke(_coordinate);
            Destroy(gameObject);
        }
    }

    private async void UpdateMesh()
    {
        _isUpdating = true;
        try
        {
            Mesh newMesh = await BuildMeshTask();
            if (newMesh != null)
            {
                // --- THE FIX ---
                // Get the DIRECT reference to the mesh currently in use. No copy is made.
                Mesh oldMesh = _meshFilter.sharedMesh;
                if (_meshCollider != null)
                {
                    _meshCollider.sharedMesh = null; // Important! Unlink logic
                }

                // Assign the new mesh directly.
                _meshFilter.sharedMesh = newMesh;
                if (_meshCollider != null)
                {
                    _meshCollider.sharedMesh = newMesh;
                }

                // Now, this destroys the ACTUAL old mesh, preventing the leak.
                if (oldMesh != null)
                {
                    Destroy(oldMesh,0.1f);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"An error occurred while updating chunk {_coordinate}.");
            Debug.LogException(e);
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task<Mesh> BuildMeshTask()
    {
        var mesher = new Mesher();
        Chunk chunkData = null;
        Mesh finalMesh = null;
        JobHandle? jobHandle = null; // Track the handle

        try
        {
            var voxelOrigin = ChunkCoordToVoxelOrigin(_coordinate);
            chunkData = await _voxelProvider.GetVoxelDataForChunk(voxelOrigin, new Vector3Int(Chunk.ChunkSizeX, Chunk.ChunkSizeY, Chunk.ChunkSizeZ));
            if (chunkData == null)
            {
                _onBuildFailed?.Invoke(_coordinate);
                Destroy(gameObject);
                return null;
            }
            jobHandle = mesher.StartMeshJob(chunkData, Mesher.Mode.Simd32Multithreaded);
            mesher.WaitForMeshJob();
            if (mesher.Vertices.Length > 2)
            {
                Vector3 unrotatedPosition = ChunkVoxelOriginToLocalPos(voxelOrigin);
                transform.position = _cameraRig.trackingSpace.TransformPoint(unrotatedPosition);
                //transform.rotation = _cameraRig.trackingSpace.rotation * Quaternion.Euler(0, -90, 0);
                float scale = _environmentMapper.metersPerVoxel;
                float gapCorrectionScale = (float)Chunk.ChunkSizeX / (Chunk.ChunkSizeX - 1.0f);
                transform.localScale = new Vector3(scale, scale, scale) * gapCorrectionScale;
                finalMesh = new Mesh();
                finalMesh.SetMesh(mesher);
                finalMesh.triangles = finalMesh.triangles.Reverse().ToArray();
                finalMesh.RecalculateNormals();
                finalMesh.RecalculateBounds();
            }
            else
            {
                _onBuildFailed?.Invoke(_coordinate);
                Destroy(gameObject);
                return null;
            }
        }
        catch (Exception e)
        {
            // If we crash here, we MUST ensure the job is dead before we dispose chunkData
            if (jobHandle.HasValue) jobHandle.Value.Complete();

            Debug.LogError($"Error in chunk {_coordinate}: {e.Message}");
            // ... handling ...
        }
        finally
        {
            mesher?.Dispose();
            chunkData?.Dispose();
        }
        return finalMesh;
    }

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