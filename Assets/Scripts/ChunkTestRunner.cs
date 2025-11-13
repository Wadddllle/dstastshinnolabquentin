using UnityEngine;
using MarchingCubes; // Namespace from Keijiro's scripts
using Anaglyph.XRTemplate; // Namespace from Lasertag's EnvironmentMapper
using System.Threading.Tasks;
using UnityEngine.Rendering;


public class ChunkTestRunner : MonoBehaviour
{
    [SerializeField] private EnvironmentMapper _environmentMapper;
    [SerializeField] private GameObject _chunkPrefab;
    [SerializeField] private float _updateInterval = 1.0f;
    [SerializeField] private ComputeShader _directReadbackShader;


    private ChunkController testChunk;

    void Start()
    {
        // Define the properties for our single test chunk
        float metersPerVoxel = _environmentMapper.metersPerVoxel;
        RenderTexture globalVolume = _environmentMapper.volume;

        Vector3Int chunkDimensionsInVoxels = new Vector3Int(
            Mathf.RoundToInt(2.0f / metersPerVoxel),  // 2 meters wide
            globalVolume.height,                      // Full height of the global volume
            Mathf.RoundToInt(2.0f / metersPerVoxel)   // 2 meters deep
        );

        // We want to create the chunk that sits right at the center of the world.
        // In the shader, world (0,0,0) maps to the middle of the volume.
        // So, we need to calculate the voxel offset to get to that middle section.
        Vector3Int globalVolumeCenterInVoxels = new Vector3Int(
            globalVolume.width / 2,
            globalVolume.height / 2,
            globalVolume.depth / 2
            );

        // Our chunk's origin (bottom-front-left corner in voxels)
        // should be centered around this point.
        Vector3Int chunkVoxelOrigin = new Vector3Int(
            globalVolumeCenterInVoxels.x - chunkDimensionsInVoxels.x / 2,
            0, // Start at the floor
            globalVolumeCenterInVoxels.z - chunkDimensionsInVoxels.z / 2
        );

        // For now, our chunk's logical "coordinate" is its origin in the voxel grid.
        Vector3Int chunkCoordForInit = chunkVoxelOrigin;

        // Spawn the chunk GameObject
        GameObject chunkGO = Instantiate(_chunkPrefab);
        chunkGO.name = "Test_Chunk_Centered";
        // Its position in the world should be the world position of its origin.
        // We can get this by running our logic in reverse.
        Vector3 chunkWorldPosition = new Vector3(
            (chunkVoxelOrigin.x - globalVolumeCenterInVoxels.x) * metersPerVoxel,
            (chunkVoxelOrigin.y - globalVolumeCenterInVoxels.y) * metersPerVoxel,
            (chunkVoxelOrigin.z - globalVolumeCenterInVoxels.z) * metersPerVoxel
        );
        chunkGO.transform.position = chunkWorldPosition;

        // --- Initialize the chunk controller ---
        // It needs its coordinate (origin), dimensions, and scale
        testChunk = chunkGO.GetComponent<ChunkController>();
        testChunk.Initialize(chunkCoordForInit, chunkDimensionsInVoxels, metersPerVoxel);

        InvokeRepeating(nameof(UpdateChunkMesh), 2.0f, _updateInterval);
    }

    //void UpdateChunkMesh()
    //{
    //    Debug.Log("Updating test chunk mesh...");
    //    testChunk.GenerateMeshFromGlobalVolume(_environmentMapper.volume);
    //}

    async void UpdateChunkMesh()
    {
        float centerVoxelValue = await ReadCenterVoxelFromGlobalVolume(_environmentMapper.volume);
        Debug.Log($"<color=yellow>DIAGNOSTIC RESULT: Value at the center of the global volume is {centerVoxelValue}.</color>");

        if (centerVoxelValue == 0)
        {
            Debug.LogError("The global TSDF volume is filled with ZEROs, not the expected ONEs. This is the root cause. Check the EnvironmentMapper's Clear kernel and initialization timing.", this);
            return; // Halt execution
        }

        Debug.Log("--- Frame Start: Requesting diagnostic update ---");
        await testChunk.DiagnoseAndGenerateMesh(_environmentMapper.volume);
    }
    // Add this entire new function to the script
    private async Task<float> ReadCenterVoxelFromGlobalVolume(RenderTexture globalVolume)
    {
        Debug.Log($"--- Performing direct readback of global volume center ---");

        // Safety check to make sure the shader is assigned
        if (_directReadbackShader == null)
        {
            Debug.LogError("The 'Direct Readback Shader' is not assigned in the ChunkTestRunner's Inspector!", this);
            return -998; // Return a unique error code
        }

        // Define the center coordinate to read
        var readCoord = new Vector3Int(globalVolume.width / 2, globalVolume.height / 2, globalVolume.depth / 2);

        // We create a tiny 1-element ComputeBuffer to receive the single float value.
        var tinyBuffer = new ComputeBuffer(1, sizeof(float));

        int kernel = _directReadbackShader.FindKernel("CSMain");
        _directReadbackShader.SetTexture(kernel, "Source", globalVolume);
        _directReadbackShader.SetBuffer(kernel, "Result", tinyBuffer);
        _directReadbackShader.SetInts("ReadCoord", new int[] { readCoord.x, readCoord.y, readCoord.z });
        _directReadbackShader.Dispatch(kernel, 1, 1, 1);

        var request = AsyncGPUReadback.Request(tinyBuffer);
        while (!request.done)
        {
            await Task.Yield();
        }

        // IMPORTANT: Always release temporary buffers to prevent memory leaks.
        tinyBuffer.Release();

        if (request.hasError)
        {
            Debug.LogError("Direct readback of global volume failed!");
            return -999;
        }

        // Return the single float that was read back from the buffer.
        return request.GetData<float>()[0];
    }
}