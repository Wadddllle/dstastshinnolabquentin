using UnityEngine;
using MarchingCubes; // Your Keijiro namespace

using System.Threading.Tasks;
using UnityEngine.Rendering;


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class ChunkController : MonoBehaviour
{
    [Header("Meshing Assets")]
    [SerializeField] private ComputeShader _conversionShader;
    [SerializeField] private ComputeShader _marchingCubesShader;
    [SerializeField] private Material _meshMaterial;

    // --- Chunk Specific Data ---
    private Vector3Int _chunkCoordinate;
    private Vector3Int _chunkVoxelDimensions;
    private float _metersPerVoxel;

    // --- Core Components ---
    private MeshBuilder _meshBuilder;
    private ComputeBuffer _voxelBuffer;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;

    public void Initialize(Vector3Int coordinate, Vector3Int dimensions, float metersPerVoxel)
    {
        _chunkCoordinate = coordinate;
        _chunkVoxelDimensions = dimensions;
        _metersPerVoxel = metersPerVoxel;

        // Setup components
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        GetComponent<MeshRenderer>().material = _meshMaterial;

        // Initialize Keijiro's MeshBuilder for THIS CHUNK'S size
        int triangleBudget = 10000; // Small budget for one chunk (e.g., 10k triangles)
        _meshBuilder = new MeshBuilder(_chunkVoxelDimensions, triangleBudget, _marchingCubesShader);
        _meshFilter.mesh = _meshBuilder.Mesh;

        // The buffer only needs to be large enough to hold one chunk's worth of voxels
        int totalVoxelsInChunk = dimensions.x * dimensions.y * dimensions.z;
        _voxelBuffer = new ComputeBuffer(totalVoxelsInChunk, sizeof(float));
    }

    // The main function that does the work!
    public async Task GenerateMeshFromGlobalVolume(RenderTexture globalVolume)
    {
        // --- THIS IS THE FIX ---
        // The coordinate we get from Initialize IS our final voxel offset. No more math needed.
        Vector3Int voxelOffset = _chunkCoordinate;
        // ----------------------

        // For debugging, let's print the values to make sure they are sane.
        // A correct offset might be around (502, 0, 502).
        // An incorrect offset would be (10040, 0, 10040).
        // Debug.Log($"Generating mesh for chunk with Voxel Offset: {voxelOffset} and Dimensions: {_chunkVoxelDimensions}");

        var globalVolumeDims = new Vector3Int(globalVolume.width, globalVolume.height, globalVolume.volumeDepth);

        // 2. Run the CONVERSION shader with the now-correct offset
        int kernel = _conversionShader.FindKernel("ConvertChunk");

        // Double-check this kernel name matches your #pragma kernel name EXACTLY (case-sensitive!)
        if (kernel < 0)
        {
            Debug.LogError("Could not find the 'ConvertChunk' kernel! Check for typos in the shader.", this);
            return;
        }

        _conversionShader.SetTexture(kernel, "TSDFVolume", globalVolume);
        _conversionShader.SetInts("GlobalDims", new int[] { globalVolumeDims.x, globalVolumeDims.y, globalVolumeDims.z });
        _conversionShader.SetInts("LocalDims", new int[] { _chunkVoxelDimensions.x, _chunkVoxelDimensions.y, _chunkVoxelDimensions.z });
        _conversionShader.SetInts("VoxelOffset", new int[] { voxelOffset.x, voxelOffset.y, voxelOffset.z });
        _conversionShader.SetBuffer(kernel, "VoxelBuffer", _voxelBuffer);

        // Dispatch using the extension method from Keijiro's library
        _conversionShader.DispatchThreads(kernel, _chunkVoxelDimensions);

        // 3. Run Marching Cubes
        _meshBuilder.BuildIsosurface(_voxelBuffer, 0.0f, _metersPerVoxel);


        await _meshBuilder.CopyMeshFromGPUAsync();

        // 4. Apply to collider
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _meshFilter.mesh;
    }
    void OnDestroy()
    {
        _meshBuilder?.Dispose();
        _voxelBuffer?.Release();
    }
    public async Task ReadbackAndDebugVoxelBuffer()
    {
        // Make sure the buffer is valid
        if (_voxelBuffer == null || _voxelBuffer.count == 0)
        {
            Debug.LogWarning("Voxel Buffer is not initialized.");
            return;
        }

        Debug.Log($"--- Reading back voxel buffer with {_voxelBuffer.count} voxels ---");

        // Request the data from the GPU asynchronously
        var request = AsyncGPUReadback.Request(_voxelBuffer);

        // Wait until the GPU has finished the request
        while (!request.done)
        {
            await Task.Yield();
        }

        if (request.hasError)
        {
            Debug.LogError("GPU readback failed.");
            return;
        }

        // Get the data into a CPU-side array
        var data = request.GetData<float>();

        // --- ANALYZE THE DATA ---
        int nonOneCount = 0;
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < data.Length; i++)
        {
            float val = data[i];
            if (val < 0.99f) // TSDF values are usually between -1 and 1. Default is 1.
            {
                nonOneCount++;
            }
            if (val < minValue) minValue = val;
            if (val > maxValue) maxValue = val;
        }

        Debug.Log($"Readback complete. Min Value: {minValue}, Max Value: {maxValue}");
        Debug.Log($"Found {nonOneCount} voxels with a value different from 1 (unscanned space).");

        // Print a small sample from the middle of the buffer
        int sampleIndex = data.Length / 2;
        Debug.Log($"Sample from middle of buffer (index {sampleIndex}): {data[sampleIndex]}");
    }
    // In ChunkController.cs
    private async Task<T[]> ReadbackBufferData<T>(ComputeBuffer buffer) where T : struct
    {
        var request = AsyncGPUReadback.Request(buffer);
        while (!request.done)
        {
            await Task.Yield();
        }

        if (request.hasError)
        {
            Debug.LogError($"GPU Readback for buffer failed.");
            return null;
        }
        return request.GetData<T>().ToArray();
    }
    // In ChunkController.cs

    public async Task DiagnoseAndGenerateMesh(RenderTexture globalVolume)
    {
        Debug.Log("--- Starting Diagnostic Mesh Generation ---");

        // === STEP 1: VERIFY THE INPUT TO MARCHING CUBES ===
        Debug.Log("Step 1: Running conversion shader to fill voxel buffer...");

        // --- THIS IS THE CRITICAL FIX ---
        // The previous version had a typo here. This is the corrected line.
        var globalVolumeDims = new Vector3Int(globalVolume.width, globalVolume.height, globalVolume.volumeDepth);
        // --------------------------------

        int kernel = _conversionShader.FindKernel("ConvertChunk");
        _conversionShader.SetTexture(kernel, "TSDFVolume", globalVolume);
        _conversionShader.SetInts("GlobalDims", new int[] { globalVolumeDims.x, globalVolumeDims.y, globalVolumeDims.z });
        _conversionShader.SetInts("LocalDims", new int[] { _chunkVoxelDimensions.x, _chunkVoxelDimensions.y, _chunkVoxelDimensions.z });
        _conversionShader.SetInts("VoxelOffset", new int[] { _chunkCoordinate.x, _chunkCoordinate.y, _chunkCoordinate.z });
        _conversionShader.SetBuffer(kernel, "VoxelBuffer", _voxelBuffer);
        _conversionShader.DispatchThreads(kernel, _chunkVoxelDimensions);

        // Read it back immediately to see if it's valid
        var voxelData = await ReadbackBufferData<float>(_voxelBuffer);
        if (voxelData == null || voxelData.Length == 0) { Debug.LogError("Voxel data readback failed."); return; }

        float minVal = float.MaxValue, maxVal = float.MinValue;
        foreach (var v in voxelData) { if (v < minVal) minVal = v; if (v > maxVal) maxVal = v; }
        Debug.Log($"Step 1 Complete. Voxel buffer now has Min Value: {minVal}, Max Value: {maxVal}.");

        // === STEP 2: RUN MARCHING CUBES & VERIFY OUTPUT ===
        Debug.Log("Step 2: Running Marching Cubes...");
        _meshBuilder.BuildIsosurface(_voxelBuffer, 0.0f, _metersPerVoxel);

        var meshData = await _meshBuilder.CopyMeshFromGPUAsync();
        if (meshData == null) { Debug.LogError("Mesh data readback failed."); return; }
        Debug.Log($"Step 2 Complete. Marching cubes produced {meshData.vertices.Length} vertices.");

        // === STEP 3: APPLY THE VERIFIED DATA ===
        if (meshData.vertices.Length > 0)
        {
            Debug.Log("Step 3: Applying verified data to mesh...");
            _meshFilter.mesh.Clear();
            _meshFilter.mesh.indexFormat = meshData.vertices.Length > 65534 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            _meshFilter.mesh.SetVertices(meshData.vertices);
            _meshFilter.mesh.SetNormals(meshData.normals);
            _meshFilter.mesh.SetIndices(meshData.indices, MeshTopology.Triangles, 0, false);
            _meshFilter.mesh.Optimize();
            _meshFilter.mesh.RecalculateBounds();

            Debug.Log("<color=green>Mesh Renderer should now have a visible mesh.</color>");

            Debug.Log("Attempting to apply mesh to collider...");
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _meshFilter.mesh;
            Debug.Log("<color=green>Successfully applied mesh to MeshCollider!</color>");
        }
        else
        {
            _meshFilter.mesh.Clear(); // Ensure old mesh is gone
            _meshCollider.sharedMesh = null; // Ensure old collider is gone
            Debug.LogWarning("Marching cubes produced 0 vertices (This is CORRECT for empty space). Clearing mesh and collider.");
        }
    }
}