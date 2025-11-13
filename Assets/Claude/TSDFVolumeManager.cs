using UnityEngine;
using Meta.XR.EnvironmentDepth;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages TSDF (Truncated Signed Distance Field) volume for real-time 3D reconstruction
/// Optimized for Quest 3 using the latest Meta XR SDK v78
/// </summary>
public class TSDFVolumeManager : MonoBehaviour
{
    [Header("TSDF Configuration")]
    [SerializeField] private ComputeShader tsdfCompute;
    [SerializeField] private RenderTexture volume; // NOW ASSIGNED IN INSPECTOR!
    [SerializeField] private float metersPerVoxel = 0.1f;
    [SerializeField] private float truncationDistance = 0.1f;
    [SerializeField] private int maxWeight = 128;

    [Header("Integration Settings")]
    [SerializeField] private float integrationInterval = 0.2f; // 5 Hz
    [SerializeField] private EnvironmentDepthManager depthManager;
    [SerializeField] private Camera centerEyeCamera;

    [Header("Optimization")]
    [SerializeField] private bool useFrustumCulling = true;
    [SerializeField] private float minDepth = 0.3f;
    [SerializeField] private float maxDepth = 5.0f;

    [Header("Player Culling")]
    [SerializeField] private Transform[] playerTransforms;
    [SerializeField] private float playerRadius = 0.5f;
    [SerializeField] private float playerHeight = 1.7f;

    // Compute shader kernel IDs
    private int clearKernel;
    private int integrateKernel;
    // Removed raymarch - not used yet

    // 3D Volume texture (stores TSDF values)
    private RenderTexture tsdfVolume; // Reference to assigned volume

    // Volume dimensions (from assigned texture)
    private int vWidth => volume.width;
    private int vHeight => volume.height;
    private int vDepth => volume.volumeDepth;

    // Shader property IDs for better performance
    private static readonly int VolumeTexID = Shader.PropertyToID("_TSDFVolume");
    private static readonly int DepthTexID = Shader.PropertyToID("_EnvironmentDepthTexture");
    private static readonly int ViewMatrixID = Shader.PropertyToID("_ViewMatrix");
    private static readonly int ProjMatrixID = Shader.PropertyToID("_ProjMatrix");
    private static readonly int InvViewMatrixID = Shader.PropertyToID("_InvViewMatrix");
    private static readonly int InvProjMatrixID = Shader.PropertyToID("_InvProjMatrix");
    private static readonly int VolumeOriginID = Shader.PropertyToID("_VolumeOrigin");
    private static readonly int VolumeSizeID = Shader.PropertyToID("_VolumeSize");
    private static readonly int VoxelSizeID = Shader.PropertyToID("_VoxelSize");
    private static readonly int TruncationDistID = Shader.PropertyToID("_TruncationDistance");
    private static readonly int MaxWeightID = Shader.PropertyToID("_MaxWeight");
    private static readonly int DepthRangeID = Shader.PropertyToID("_DepthRange");
    private static readonly int NumPlayersID = Shader.PropertyToID("_NumPlayers");
    private static readonly int PlayerPositionsID = Shader.PropertyToID("_PlayerPositions");
    private static readonly int PlayerRadiusID = Shader.PropertyToID("_PlayerRadius");
    private static readonly int PlayerHeightID = Shader.PropertyToID("_PlayerHeight");
    private static readonly int FrustumVoxelsID = Shader.PropertyToID("_FrustumVoxels");
    private static readonly int NumFrustumVoxelsID = Shader.PropertyToID("_NumFrustumVoxels");

    // Frustum culling buffer
    private ComputeBuffer frustumPointsBuffer;
    private int frustumPointCount;

    private bool isInitialized = false;
    private Coroutine integrationCoroutine;

    private void Awake()
    {
        // Auto-find components if not assigned
        if (depthManager == null)
            depthManager = FindFirstObjectByType<EnvironmentDepthManager>();

        if (centerEyeCamera == null)
        {
            // Try to find center eye camera (OVR specific)
            var cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (cameraRig != null)
                centerEyeCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();

            // Fallback to main camera
            if (centerEyeCamera == null)
                centerEyeCamera = Camera.main;
        }

        if (centerEyeCamera == null)
        {
            Debug.LogError("TSDFVolumeManager: No camera found! Please assign centerEyeCamera.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (!ValidateSetup())
            return;

        Debug.Log("Starting TSDF initialization...");

        InitializeTSDFVolume();
        ClearVolume();

        isInitialized = true;

        Debug.Log("TSDF system fully initialized and ready");
    }

    private bool ValidateSetup()
    {
        if (tsdfCompute == null)
        {
            Debug.LogError("TSDF Compute Shader is not assigned!");
            enabled = false;
            return false;
        }

        if (volume == null)
        {
            Debug.LogError("Volume RenderTexture is not assigned! Create a 3D RenderTexture asset and assign it.");
            enabled = false;
            return false;
        }

        if (depthManager == null)
        {
            Debug.LogError("EnvironmentDepthManager not found!");
            enabled = false;
            return false;
        }

        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogError("Environment Depth is not supported on this device!");
            enabled = false;
            return false;
        }

        if (centerEyeCamera == null)
        {
            Debug.LogError("Center Eye Camera not found!");
            enabled = false;
            return false;
        }

        Debug.Log($"TSDF System validated. Volume: {vWidth}x{vHeight}x{vDepth}, Camera: {centerEyeCamera.name}");
        return true;
    }

    private void InitializeTSDFVolume()
    {
        Debug.Log("InitializeTSDFVolume called");

        // Find kernel IDs
        clearKernel = tsdfCompute.FindKernel("Clear");
        integrateKernel = tsdfCompute.FindKernel("Integrate");

        Debug.Log($"Kernels found - Clear: {clearKernel}, Integrate: {integrateKernel}");

        // Use assigned volume
        tsdfVolume = volume;

        if (tsdfVolume == null)
        {
            Debug.LogError("Volume assignment failed! Volume is null!");
            return;
        }

        Debug.Log($"Volume assigned: {tsdfVolume.name}, IsCreated: {tsdfVolume.IsCreated()}");

        if (!tsdfVolume.IsCreated())
        {
            Debug.Log("Creating volume...");
            tsdfVolume.Create();
        }

        // Set global volume parameters
        tsdfCompute.SetInts("volumeSize", vWidth, vHeight, vDepth);
        tsdfCompute.SetFloat(nameof(metersPerVoxel), metersPerVoxel);

        Debug.Log($"Volume parameters set: {vWidth}x{vHeight}x{vDepth}, {metersPerVoxel}m/voxel");

        // ALWAYS initialize frustum culling (we need the buffer)
        Debug.Log("Initializing frustum culling...");
        InitializeFrustumCulling();

        Debug.Log($"TSDF Volume initialized: ({vWidth}, {vHeight}, {vDepth}) at {metersPerVoxel}m/voxel");
    }

    private void InitializeFrustumCulling()
    {
        // LaserTag-style frustum generation
        // Generate voxel positions in VIEW SPACE (not world space!)
        Vector3Int resolution = new Vector3Int(vWidth, vHeight, vDepth);

        List<Vector3> positions = new List<Vector3>(200000);

        // Generate frustum-shaped voxel cloud in view space
        float zNear = minDepth;
        float zFar = maxDepth;

        // Simple approach: generate all voxels, filter by distance
        for (float z = zNear; z < zFar; z += metersPerVoxel)
        {
            // Approximate frustum shape
            float xRange = z * 0.5f; // Adjust based on FOV
            float yRange = z * 0.5f;

            for (float x = -xRange; x < xRange; x += metersPerVoxel)
            {
                for (float y = -yRange; y < yRange; y += metersPerVoxel)
                {
                    Vector3 v = new Vector3(x, y, -z); // View space (camera looks down -Z)

                    float dist = v.magnitude;
                    if (dist > minDepth && dist < maxDepth)
                    {
                        positions.Add(v);
                    }
                }
            }
        }

        frustumPointCount = positions.Count;
        frustumPointsBuffer = new ComputeBuffer(frustumPointCount, sizeof(float) * 3);
        frustumPointsBuffer.SetData(positions);

        Debug.Log($"Frustum culling: {frustumPointCount} voxels in view frustum");
    }

    public void ClearVolume()
    {
        if (tsdfVolume == null)
        {
            Debug.LogError("Cannot clear volume - volume not assigned!");
            return;
        }

        Debug.Log("Clearing TSDF volume...");

        tsdfCompute.SetTexture(clearKernel, "volume", tsdfVolume);

        int threadGroupsX = Mathf.CeilToInt(vWidth / 4f);
        int threadGroupsY = Mathf.CeilToInt(vHeight / 4f);
        int threadGroupsZ = Mathf.CeilToInt(vDepth / 4f);

        tsdfCompute.Dispatch(clearKernel, threadGroupsX, threadGroupsY, threadGroupsZ);

        Debug.Log($"TSDF Volume cleared: {threadGroupsX}x{threadGroupsY}x{threadGroupsZ} thread groups");
    }

    private void OnEnable()
    {
        if (isInitialized)
            integrationCoroutine = StartCoroutine(IntegrationLoop());
    }

    private void OnDisable()
    {
        if (integrationCoroutine != null)
        {
            StopCoroutine(integrationCoroutine);
            integrationCoroutine = null;
        }
    }

    private IEnumerator IntegrationLoop()
    {
        var wait = new WaitForSeconds(integrationInterval);
        int integrationCount = 0;

        while (true)
        {
            yield return wait;

            if (depthManager != null && depthManager.IsDepthAvailable)
            {
                IntegrateDepthFrame();
                integrationCount++;

                if (integrationCount % 10 == 0)
                {
                    Debug.Log($"TSDF Integration: {integrationCount} frames integrated");
                }
            }
            else
            {
                if (integrationCount % 10 == 0)
                {
                    Debug.LogWarning("Depth not available, skipping integration");
                }
            }
        }
    }

    private void IntegrateDepthFrame()
    {
        // Get depth texture from global shader properties
        Texture depthTexture = Shader.GetGlobalTexture("_EnvironmentDepthTexture");

        if (depthTexture == null)
        {
            return;
        }

        // Calculate camera matrices
        Matrix4x4 viewMatrix = centerEyeCamera.worldToCameraMatrix;
        Matrix4x4 projMatrix = centerEyeCamera.projectionMatrix;

        // Set shader parameters (LaserTag style - simpler!)
        tsdfCompute.SetTexture(integrateKernel, "volume", tsdfVolume);
        tsdfCompute.SetTexture(integrateKernel, "_EnvironmentDepthTexture", depthTexture);

        tsdfCompute.SetMatrix("_ViewMatrix", viewMatrix);
        tsdfCompute.SetMatrix("_ProjMatrix", projMatrix);
        tsdfCompute.SetMatrix("_InvViewMatrix", viewMatrix.inverse);
        tsdfCompute.SetMatrix("_InvProjMatrix", projMatrix.inverse);

        tsdfCompute.SetFloat("_TruncationDistance", truncationDistance);
        tsdfCompute.SetInt("_MaxWeight", maxWeight);
        tsdfCompute.SetVector("_DepthRange", new Vector2(minDepth, maxDepth));

        // Player culling
        if (playerTransforms != null && playerTransforms.Length > 0)
        {
            Vector4[] playerPositions = new Vector4[16];
            int numPlayers = Mathf.Min(playerTransforms.Length, 16);

            for (int i = 0; i < numPlayers; i++)
            {
                if (playerTransforms[i] != null)
                    playerPositions[i] = playerTransforms[i].position;
            }

            tsdfCompute.SetInt("_NumPlayers", numPlayers);
            tsdfCompute.SetVectorArray("_PlayerPositions", playerPositions);
            tsdfCompute.SetFloat("_PlayerRadius", playerRadius);
            tsdfCompute.SetFloat("_PlayerHeight", playerHeight);
        }
        else
        {
            tsdfCompute.SetInt("_NumPlayers", 0);
        }

        // Set frustum buffer
        tsdfCompute.SetBuffer(integrateKernel, "frustumVolume", frustumPointsBuffer);

        // Dispatch
        int threadGroups = Mathf.CeilToInt(frustumPointCount / 64f);
        tsdfCompute.Dispatch(integrateKernel, threadGroups, 1, 1);
    }

    private void OnDestroy()
    {
        if (tsdfVolume != null)
        {
            tsdfVolume.Release();
            Destroy(tsdfVolume);
        }

        if (frustumPointsBuffer != null)
        {
            frustumPointsBuffer.Release();
            frustumPointsBuffer.Dispose();
        }
    }

    // Public API
    public RenderTexture GetTSDFVolume() => tsdfVolume;
    public Vector3Int GetResolution() => new Vector3Int(vWidth, vHeight, vDepth);
    public float GetMetersPerVoxel() => metersPerVoxel;
    public bool IsInitialized() => isInitialized && tsdfVolume != null;

    private void OnDrawGizmos()
    {
        // Visualize TSDF volume bounds
        Gizmos.color = Color.cyan;
        // FIX: Define and calculate the volume's size in world space
        Vector3 worldVolumeSize = new Vector3(vWidth * metersPerVoxel, vHeight * metersPerVoxel, vDepth * metersPerVoxel);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, worldVolumeSize);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}