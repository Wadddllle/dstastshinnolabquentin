using UnityEngine;

public class TSDFManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField, Range(0.01f, 0.2f)]
    private float metersPerVoxel = 0.05f;

    [Header("References")]
    [SerializeField] private ComputeShader tsdfShader;
    [SerializeField] private RenderTexture tsdfVolume;
    private Camera _mainCamera;

    [Header("Debugging")]
    public bool _DebugMode = false;
    private int _integrateKernel;

    // --- THIS IS THE NEW "ANCHOR" ---
    private Vector3 _volumeCenter;

    void Start()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("TSDFManager requires a Main Camera!", this);
            this.enabled = false;
            return;
        }

        if (tsdfShader == null || tsdfVolume == null)
        {
            Debug.LogError("TSDF Shader or Volume is not assigned!", this);
            this.enabled = false;
            return;
        }

        // --- ANCHOR THE VOLUME ---
        // Save the initial position of this GameObject when the scene starts.
        _volumeCenter = transform.position;
        Debug.Log($"TSDF Volume anchored at world position: {_volumeCenter}");

        _integrateKernel = tsdfShader.FindKernel("Integrate");

        var volSize = new Vector3(tsdfVolume.width, tsdfVolume.height, tsdfVolume.volumeDepth);
        tsdfShader.SetVector("_VolumeSize", volSize);
        tsdfShader.SetFloat("_MetersPerVoxel", metersPerVoxel);

        // It's a good idea to clear the volume at the start.
        ClearVolume();
    }

    void Update()
    {
        var depthTexture = Shader.GetGlobalTexture(Shader.PropertyToID("_EnvironmentDepthTexture"));
        if (depthTexture == null)
        {
            return; // Wait for depth data
        }

        tsdfShader.SetTexture(_integrateKernel, "_EnvironmentDepthTexture", depthTexture);
        tsdfShader.SetTexture(_integrateKernel, "_TSDFVolume", tsdfVolume);

        tsdfShader.SetMatrix("_CameraView", _mainCamera.worldToCameraMatrix);
        tsdfShader.SetMatrix("_CameraProjection", _mainCamera.projectionMatrix);

        // --- USE THE ANCHORED POSITION ---
        // Instead of the dynamic 'transform.position', we use our saved, static '_volumeCenter'.
        tsdfShader.SetVector("_VolumeCenter", _volumeCenter);

        tsdfShader.SetInt("_DebugMode", _DebugMode ? 1 : 0);

        tsdfShader.Dispatch(_integrateKernel, tsdfVolume.width / 8, tsdfVolume.height / 8, tsdfVolume.volumeDepth / 8);
    }

    // Add a clear function to easily reset the volume
    [ContextMenu("Clear Volume")]
    public void ClearVolume()
    {
        int kernel = tsdfShader.FindKernel("ClearVolume"); // Assuming you add this simple kernel
        tsdfShader.SetVector("_VolumeSize", new Vector3(tsdfVolume.width, tsdfVolume.height, tsdfVolume.volumeDepth));
        tsdfShader.SetTexture(kernel, "_TSDFVolume", tsdfVolume);
        tsdfShader.Dispatch(kernel, tsdfVolume.width / 8, tsdfVolume.height / 8, tsdfVolume.volumeDepth / 8);
    }
}