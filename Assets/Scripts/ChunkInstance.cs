using UnityEngine;
using System.Threading.Tasks;
using MarchingCubes;
using Unity.Collections;
using System;
using System.Linq;
using Anaglyph.XRTemplate;
public class ChunkInstance : MonoBehaviour
{
    // --- State & Dependencies ---
    private Vector3Int _coordinate;
    private Action<Vector3Int> _onBuildFailed;
    private VoxelProvider _voxelProvider;
    private Material _meshMaterial;
    private EnvironmentMapper _environmentMapper;
    private OVRCameraRig _cameraRig;

    private bool _isUpdating = false; // Prevents spamming update requests

    // --- Components ---
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    public void Initialize(Vector3Int coordinate, Action<Vector3Int> onBuildFailed, VoxelProvider provider, Material material, EnvironmentMapper mapper, OVRCameraRig cameraRig)
    {
        _coordinate = coordinate;
        _onBuildFailed = onBuildFailed;
        _voxelProvider = provider;
        _meshMaterial = material;
        _environmentMapper = mapper;
        _cameraRig = cameraRig;

        gameObject.name = $"Chunk_{_coordinate.x}_{_coordinate.y}_{_coordinate.z}";

        // Start the very first mesh build process
        InitialBuild();
    }

    // --- Public method for the manager to call ---
    public void TriggerUpdate()
    {
        if (_isUpdating) return; // Don't start a new update if one is already running
        UpdateMesh();
    }

    private async void InitialBuild()
    {
        try
        {
            Mesh newMesh = await BuildMeshTask();
            if (newMesh != null)
            {
                // On the first build, we ADD the components
                _meshFilter = gameObject.AddComponent<MeshFilter>();
                gameObject.AddComponent<MeshRenderer>().material = _meshMaterial;
                _meshCollider = gameObject.AddComponent<MeshCollider>();

                _meshFilter.mesh = newMesh;
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
                // --- THE SEAMLESS SWAP ---
                // 1. Get the old mesh to destroy it later
                Mesh oldMesh = _meshFilter.mesh;

                // 2. Assign the new mesh to the existing components
                _meshFilter.mesh = newMesh;
                _meshCollider.sharedMesh = newMesh;

                // 3. Clean up the old mesh data to prevent memory leaks
                if (oldMesh != null)
                {
                    Destroy(oldMesh);
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

    // This is the core, reusable task that builds and returns a mesh.
    private async Task<Mesh> BuildMeshTask()
    {
        var mesher = new Mesher();
        Chunk chunkData = null;
        Mesh finalMesh = null;

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

            mesher.StartMeshJob(chunkData, Mesher.Mode.Naive);
            mesher.WaitForMeshJob();

            if (mesher.Vertices.Length > 2)
            {
                // Set transform properties on the GameObject itself
                Vector3 unrotatedPosition = ChunkVoxelOriginToLocalPos(voxelOrigin);
                transform.position = _cameraRig.trackingSpace.TransformPoint(unrotatedPosition);
                transform.rotation = _cameraRig.trackingSpace.rotation * Quaternion.Euler(0, -90, 0);
                float scale = _environmentMapper.metersPerVoxel;

                // --- THIS IS THE FIX ---
                // Calculate the precise scale needed to close the 1-voxel gap.
                float gapCorrectionScale = (float)Chunk.ChunkSizeX / (Chunk.ChunkSizeX - 1.0f); // This is 32.0f / 31.0f

                // Apply the base scale and the gap correction scale.
                transform.localScale = new Vector3(scale, scale, -scale) * gapCorrectionScale;
                // --- END FIX ---


                finalMesh = new Mesh();
                finalMesh.SetMesh(mesher);
                finalMesh.triangles = finalMesh.triangles.Reverse().ToArray();
                finalMesh.RecalculateNormals();
                finalMesh.RecalculateBounds();
            }
            else
            {
                // If the updated mesh is empty, the chunk should be removed.
                _onBuildFailed?.Invoke(_coordinate);
                Destroy(gameObject);
                return null;
            }
        }
        finally
        {
            mesher?.Dispose();
            chunkData?.Dispose();
        }
        return finalMesh;
    }

    // --- Coordinate Functions ---
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