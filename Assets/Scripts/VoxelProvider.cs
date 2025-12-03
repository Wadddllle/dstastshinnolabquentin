using UnityEngine;
using System.Threading.Tasks;
using Unity.Collections;
using Anaglyph.XRTemplate;
using UnityEngine.Rendering;
using MarchingCubes; // We now need the MarchingCubes namespace to know what a "Chunk" is.

public class VoxelProvider : MonoBehaviour
{
    [Header("Source Data")]
    [SerializeField] private EnvironmentMapper _environmentMapper;

    [Header("Conversion")]
    [SerializeField] private ComputeShader _convertChunkShader;

    // --- CHANGE 1: The return type is now Task<Chunk> ---
    public async Task<Chunk> GetVoxelDataForChunk(Vector3Int chunkVoxelOrigin, Vector3Int chunkDimensions)
    {
        int totalVoxelsInChunk = chunkDimensions.x * chunkDimensions.y * chunkDimensions.z;
        int intCount = Mathf.CeilToInt(totalVoxelsInChunk / 4.0f);

        //ComputeBuffer voxelBufferFloats = new ComputeBuffer(totalVoxelsInChunk, sizeof(float));
        ComputeBuffer voxelBufferInts = new ComputeBuffer(intCount, sizeof(int));


        var globalVolume = _environmentMapper.volume;
        var globalVolumeDims = new Vector3Int(globalVolume.width, globalVolume.height, globalVolume.volumeDepth);

        int kernel = _convertChunkShader.FindKernel("ConvertChunk");
        _convertChunkShader.SetTexture(kernel, "TSDFVolume", globalVolume);
        _convertChunkShader.SetInts("GlobalDims", new int[] { globalVolumeDims.x, globalVolumeDims.y, globalVolumeDims.z });
        _convertChunkShader.SetInts("LocalDims", new int[] { chunkDimensions.x, chunkDimensions.y, chunkDimensions.z });
        _convertChunkShader.SetInts("VoxelOffset", new int[] { chunkVoxelOrigin.x, chunkVoxelOrigin.y, chunkVoxelOrigin.z });
        //_convertChunkShader.SetBuffer(kernel, "VoxelBuffer", voxelBufferFloats);
        _convertChunkShader.SetBuffer(kernel, "VoxelBuffer", voxelBufferInts);

        int threadGroupsZ = Mathf.CeilToInt((chunkDimensions.z / 4.0f) / 2.0f);

        _convertChunkShader.Dispatch(kernel,
            Mathf.CeilToInt(chunkDimensions.x / 8.0f),
            Mathf.CeilToInt(chunkDimensions.y / 8.0f),
            threadGroupsZ
            );
        //int threadGroupsX = Mathf.CeilToInt(chunkDimensions.x / 8.0f);
        //int threadGroupsY = Mathf.CeilToInt(chunkDimensions.y / 8.0f);
        //int threadGroupsZ = Mathf.CeilToInt(chunkDimensions.z / 8.0f);
        //_convertChunkShader.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        //var request = AsyncGPUReadback.Request(voxelBufferFloats);

        //while (!request.done)
        //{
        //    if (request.hasError)
        //    {
        //        Debug.LogError("VoxelProvider: GPU Readback failed during wait!");
        //        voxelBufferFloats.Release();
        //        return null; // Return null on failure
        //    }
        //    await Task.Yield();
        //}

        //voxelBufferFloats.Release();

        //if (request.hasError)
        //{
        //    Debug.LogError("VoxelProvider: GPU Readback failed!");
        //    return null; // Return null on failure
        //}

        //var floatData = request.GetData<float>();

        //// --- CHANGE 2: Create a new Chunk object to hold the data ---
        //var chunk = new Chunk();

        //// --- CHANGE 3: Fill the chunk's data array directly ---
        //for (int i = 0; i < floatData.Length; i++)
        //{
        //    sbyte convertedValue = (sbyte)Mathf.Clamp(floatData[i] * 127.0f, -127.0f, 127.0f);
        //    chunk.data[i] = convertedValue;
        //}

        //Debug.Log($"VoxelProvider: Successfully provided a Chunk with {chunk.data.Length} voxels.");

        //// --- CHANGE 4: Return the fully populated Chunk ---
        //return chunk;
        var request = AsyncGPUReadback.Request(voxelBufferInts);
        while (!request.done) await Task.Yield();

        if (request.hasError) { /* Handle Error */ return null; }

        var intData = request.GetData<int>();
        var chunk = new Chunk(); // Your MarchingCubes chunk

        // MAGIC: Copy raw Int bits directly into Sbyte array. 
        // No For-Loop. 0ms execution time.
        NativeArray<int>.Copy(intData, chunk.data.Reinterpret<int>(1), intCount);

        voxelBufferInts.Release();
        return chunk;
    }
}