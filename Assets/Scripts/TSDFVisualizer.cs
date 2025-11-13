using UnityEngine;
using MarchingCubes; // Namespace from Keijiro's scripts
using Anaglyph.XRTemplate; // Namespace from Lasertag's EnvironmentMapper

// This script requires a MeshFilter and MeshRenderer to display the final result.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TSDFVisualizer : MonoBehaviour
{
    [Header("Input Data Source")]
    [Tooltip("Drag the GameObject that has the EnvironmentMapper script on it.")]
    [SerializeField] private EnvironmentMapper _environmentMapper;

    [Header("Marching Cubes Assets")]
    [Tooltip("The MarchingCubes.compute shader from Keijiro's implementation.")]
    [SerializeField] private ComputeShader _marchingCubesShader;

    [Tooltip("The conversion shader we just created (ConvertTextureToBuffer.compute).")]
    [SerializeField] private ComputeShader _conversionShader;

    [Tooltip("The material to apply to the final generated mesh.")]
    [SerializeField] private Material _meshMaterial;

    [Header("Meshing Parameters")]
    [Tooltip("Maximum number of triangles in the mesh. Increase if the mesh looks incomplete.")]
    [SerializeField] private int _triangleBudget = 2 * 1024 * 1024; // 2 million triangles

    // Private references to the core components
    private MeshBuilder _meshBuilder;
    private ComputeBuffer _voxelBuffer;
    private MeshFilter _meshFilter;

    void Start()
    {
        // --- Validation: Ensure everything is set up correctly ---
        if (_environmentMapper == null || _marchingCubesShader == null || _conversionShader == null)
        {
            Debug.LogError("One or more essential references are not assigned in the Inspector!", this);
            this.enabled = false;
            return;
        }

        // --- Initialization ---
        var volumeTexture = _environmentMapper.GetComponent<EnvironmentMapper>().volume;
        var dims = new Vector3Int(volumeTexture.width, volumeTexture.height, volumeTexture.volumeDepth);

        // 1. Create the ComputeBuffer that will act as the bridge between the two systems.
        int totalVoxels = dims.x * dims.y * dims.z;
        _voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(float));

        // 2. Initialize Keijiro's MeshBuilder with our volume dimensions and shader.
        _meshBuilder = new MeshBuilder(dims, _triangleBudget, _marchingCubesShader);

        // 3. Get the components needed for rendering and assign the mesh and material.
        _meshFilter = GetComponent<MeshFilter>();
        GetComponent<MeshRenderer>().material = _meshMaterial;
        _meshFilter.mesh = _meshBuilder.Mesh;

        Debug.Log("TSDF Visualizer initialized successfully.");
    }

    void Update()
    {
        // Every frame, we perform the full pipeline:
        // EnvironmentMapper (already running) -> Conversion -> Marching Cubes

        // 1. Run our conversion shader to copy data from the RenderTexture to the ComputeBuffer.
        var volumeTexture = _environmentMapper.GetComponent<EnvironmentMapper>().volume;
        //var volumeTexture = _environmentMapper.GetComponent<EnvironmentMapperChunk>().dynamicVolume;
        //if (volumeTexture == null || !volumeTexture.IsCreated()) return;

        var dims = new Vector3Int(volumeTexture.width, volumeTexture.height, volumeTexture.volumeDepth);

        int kernel = _conversionShader.FindKernel("Convert");
        _conversionShader.SetTexture(kernel, "TSDFVolume", volumeTexture);
        _conversionShader.SetInts("Dims", dims);
        _conversionShader.SetBuffer(kernel, "VoxelBuffer", _voxelBuffer);
        _conversionShader.DispatchThreads(kernel, dims);

        // 2. Run Keijiro's Marching Cubes algorithm on the newly filled ComputeBuffer.
        // The 'target' is 0.0f because that's the "surface" in a TSDF (the zero-crossing).
        // The 'scale' is the size of our voxels, which we get from the EnvironmentMapper.
        _meshBuilder.BuildIsosurface(_voxelBuffer, 0.0f, _environmentMapper.GetComponent<EnvironmentMapper>().metersPerVoxel);
    }

    void OnDestroy()
    {
        // IMPORTANT: Always release GPU resources when you are done with them to prevent memory leaks.
        _meshBuilder?.Dispose();
        _voxelBuffer?.Release();
    }
}