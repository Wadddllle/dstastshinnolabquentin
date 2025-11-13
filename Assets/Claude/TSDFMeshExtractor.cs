using UnityEngine;
using MarchingCubes;

/// <summary>
/// Extracts mesh from TSDF volume using Keijiro's Marching Cubes implementation
/// </summary>
[RequireComponent(typeof(TSDFVolumeManager))]
public class TSDFMeshExtractor : MonoBehaviour
{
    [Header("Marching Cubes")]
    [SerializeField] private ComputeShader marchingCubesCompute;
    [SerializeField] private ComputeShader tsdfConverterCompute; // New: TSDFConverter.compute
    [SerializeField] private int triangleBudget = 65536; // Max triangles (adjust based on performance)

    [Header("Mesh Settings")]
    [SerializeField] private Material meshMaterial;
    [SerializeField] private bool createCollider = true;
    [SerializeField] private float isoValue = 0.0f; // Surface at TSDF = 0

    [Header("Update Settings")]
    [SerializeField] private float meshUpdateInterval = 1.0f;
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private bool showDebugInfo = true;

    private TSDFVolumeManager tsdfManager;
    private MeshBuilder meshBuilder;
    private ComputeBuffer voxelBuffer;
    private int converterKernel;

    private GameObject meshObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private float lastMeshUpdate;

    private void Update()
    {
        // Manual trigger for testing (old input system)
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Manual mesh extraction triggered!");
            ExtractMesh();
        }
#endif

        // Show status in console every 5 seconds
        if (Time.frameCount % 300 == 0 && meshObject != null)
        {
            Debug.Log($"TSDFMeshExtractor status: autoUpdate={autoUpdate}, " +
                     $"lastUpdate={(Time.time - lastMeshUpdate):F1}s ago, " +
                     $"meshObject={meshObject.name}");
        }
    }

    // Public method for manual triggering (call from UI button or VR input)
    [ContextMenu("Extract Mesh Now")]
    public void TriggerManualExtraction()
    {
        Debug.Log("Manual mesh extraction triggered via public method!");
        ExtractMesh();
    }

    private void Start()
    {
        // Don't initialize immediately - wait for TSDF to be ready
        StartCoroutine(InitializeWhenReady());
    }

    private System.Collections.IEnumerator InitializeWhenReady()
    {
        Debug.Log("=== TSDFMeshExtractor waiting for TSDF Volume ===");

        tsdfManager = GetComponent<TSDFVolumeManager>();

        if (tsdfManager == null)
        {
            Debug.LogError("TSDFVolumeManager component not found on this GameObject!");
            enabled = false;
            yield break;
        }

        // Wait until TSDF volume is created AND initialized
        int waitFrames = 0;
        while (!tsdfManager.IsInitialized() || tsdfManager.GetTSDFVolume() == null)
        {
            waitFrames++;
            if (waitFrames > 300) // Increased timeout to 10 seconds at 30fps
            {
                Debug.LogError($"Timeout waiting for TSDF Volume! IsInitialized: {tsdfManager.IsInitialized()}, Volume: {tsdfManager.GetTSDFVolume()}");
                enabled = false;
                yield break;
            }

            // Log progress every second
            if (waitFrames % 30 == 0)
            {
                Debug.Log($"Still waiting... Frame {waitFrames}, IsInit: {tsdfManager.IsInitialized()}, Volume: {(tsdfManager.GetTSDFVolume() != null ? "exists" : "null")}");
            }

            yield return null;
        }

        Debug.Log($"TSDF Volume ready after {waitFrames} frames");

        if (!ValidateSetup())
            yield break;

        Debug.Log("Validation passed, initializing...");

        InitializeMarchingCubes();
        CreateMeshObject();

        Debug.Log($"Mesh object created: {meshObject.name}");
        Debug.Log($"Auto update: {autoUpdate}, interval: {meshUpdateInterval}s");

        if (autoUpdate)
        {
            InvokeRepeating(nameof(ExtractMesh), meshUpdateInterval, meshUpdateInterval);
            Debug.Log("Auto-update enabled, first mesh extraction in " + meshUpdateInterval + " seconds");
        }
    }

    private bool ValidateSetup()
    {
        if (marchingCubesCompute == null)
        {
            Debug.LogError("Marching Cubes compute shader not assigned! Assign the MeshReconstruction.compute shader.");
            enabled = false;
            return false;
        }

        if (tsdfConverterCompute == null)
        {
            Debug.LogError("TSDF Converter compute shader not assigned! Assign the TSDFConverter.compute shader.");
            enabled = false;
            return false;
        }

        if (tsdfManager == null || tsdfManager.GetTSDFVolume() == null)
        {
            Debug.LogError("TSDFVolumeManager not found or volume not initialized!");
            enabled = false;
            return false;
        }

        return true;
    }

    private void InitializeMarchingCubes()
    {
        Vector3Int resolution = tsdfManager.GetResolution();

        // Create Keijiro's MeshBuilder
        meshBuilder = new MeshBuilder(resolution, triangleBudget, marchingCubesCompute);

        // Create voxel buffer for marching cubes
        // We need to convert TSDF's RenderTexture to a ComputeBuffer of floats
        int voxelCount = resolution.x * resolution.y * resolution.z;
        voxelBuffer = new ComputeBuffer(voxelCount, sizeof(float));

        // Find converter kernel
        converterKernel = tsdfConverterCompute.FindKernel("ConvertTSDFToVoxels");

        Debug.Log($"Marching Cubes initialized: {resolution}, {triangleBudget} triangles max");
    }

    private void CreateMeshObject()
    {
        Debug.Log("Creating mesh object...");

        meshObject = new GameObject("TSDF_Mesh");
        meshObject.transform.SetParent(transform);
        meshObject.transform.localPosition = Vector3.zero;
        meshObject.transform.localRotation = Quaternion.identity;
        meshObject.transform.localScale = Vector3.one;

        Debug.Log($"Mesh GameObject created at position: {meshObject.transform.position}");

        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer = meshObject.AddComponent<MeshRenderer>();

        Debug.Log("MeshFilter and MeshRenderer added");

        if (meshMaterial != null)
        {
            meshRenderer.material = meshMaterial;
            Debug.Log($"Assigned material: {meshMaterial.name}");
        }
        else
        {
            // Create default material
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshRenderer.material.color = Color.white;
            Debug.Log("Created default Standard material");
        }

        if (createCollider)
        {
            meshCollider = meshObject.AddComponent<MeshCollider>();
            Debug.Log("MeshCollider added");
        }

        Debug.Log($"Mesh object '{meshObject.name}' fully created and ready");
    }

    /// <summary>
    /// Main extraction method - converts TSDF volume to mesh
    /// </summary>
    public void ExtractMesh()
    {
        Debug.Log("=== ExtractMesh called ===");

        if (meshBuilder == null)
        {
            Debug.LogError("meshBuilder is null!");
            return;
        }

        if (tsdfManager == null)
        {
            Debug.LogError("tsdfManager is null!");
            return;
        }

        if (tsdfManager.GetTSDFVolume() == null)
        {
            Debug.LogError("TSDF Volume is null!");
            return;
        }

        float startTime = Time.realtimeSinceStartup;

        // Step 1: Convert TSDF RenderTexture to flat float array
        Debug.Log("Converting TSDF to voxel buffer...");
        if (!ConvertTSDFToVoxelBuffer())
        {
            Debug.LogError("Failed to convert TSDF to voxel buffer");
            return;
        }

        // Step 2: Run marching cubes
        Vector3Int resolution = tsdfManager.GetResolution();
        float metersPerVoxel = tsdfManager.GetMetersPerVoxel();

        Debug.Log($"Running marching cubes: resolution={resolution}, scale={metersPerVoxel}, isoValue={isoValue}");

        meshBuilder.BuildIsosurface(voxelBuffer, isoValue, metersPerVoxel);

        // Step 3: Assign mesh to renderer
        Mesh extractedMesh = meshBuilder.Mesh;

        if (extractedMesh == null)
        {
            Debug.LogError("Marching cubes returned null mesh!");
            return;
        }

        Debug.Log($"Mesh generated: {extractedMesh.vertexCount} vertices, {extractedMesh.triangles.Length / 3} triangles");

        if (meshFilter == null)
        {
            Debug.LogError("meshFilter is null! Mesh object may not have been created.");
            return;
        }

        meshFilter.mesh = extractedMesh;
        Debug.Log("Mesh assigned to MeshFilter");

        // Step 4: Update collider if enabled
        if (createCollider && meshCollider != null)
        {
            meshCollider.sharedMesh = extractedMesh;
            Debug.Log("Mesh assigned to MeshCollider");
        }

        float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;

        Debug.Log($"=== Mesh extraction complete: {extractedMesh.vertexCount} vertices, " +
                 $"{extractedMesh.triangles.Length / 3} triangles, " +
                 $"{elapsed:F1}ms ===");

        lastMeshUpdate = Time.time;
    }

    /// <summary>
    /// Converts TSDF RenderTexture (RG32Float) to flat float buffer
    /// Marching cubes expects: float array[x + width * (y + height * z)]
    /// </summary>
    private bool ConvertTSDFToVoxelBuffer()
    {
        RenderTexture tsdfVolume = tsdfManager.GetTSDFVolume();
        if (tsdfVolume == null)
            return false;

        Vector3Int res = tsdfManager.GetResolution();

        // Use compute shader to convert TSDF 3D texture to flat buffer
        tsdfConverterCompute.SetTexture(converterKernel, "_TSDFVolume", tsdfVolume);
        tsdfConverterCompute.SetBuffer(converterKernel, "_VoxelBuffer", voxelBuffer);
        tsdfConverterCompute.SetInts("_Resolution", res.x, res.y, res.z);

        // Dispatch converter
        int threadGroupsX = Mathf.CeilToInt(res.x / 8f);
        int threadGroupsY = Mathf.CeilToInt(res.y / 8f);
        int threadGroupsZ = Mathf.CeilToInt(res.z / 8f);

        tsdfConverterCompute.Dispatch(converterKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        return true;
    }

    private void OnDestroy()
    {
        if (meshBuilder != null)
            meshBuilder.Dispose();

        if (voxelBuffer != null)
        {
            voxelBuffer.Release();
            voxelBuffer.Dispose();
        }
    }

    // Public API
    public void SetAutoUpdate(bool enabled)
    {
        autoUpdate = enabled;

        if (enabled)
            InvokeRepeating(nameof(ExtractMesh), meshUpdateInterval, meshUpdateInterval);
        else
            CancelInvoke(nameof(ExtractMesh));
    }

    public void SetIsoValue(float value)
    {
        isoValue = value;
        if (autoUpdate)
            ExtractMesh();
    }
}