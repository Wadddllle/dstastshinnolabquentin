//using UnityEngine;
//using Meta.XR.EnvironmentDepth;
//using TMPro;

///// <summary>
///// Debug helper for TSDF system - shows what's working and what's not
///// </summary>
//public class TSDFDebugger : MonoBehaviour
//{
//    [SerializeField] private TSDFVolumeManager tsdfManager;
//    [SerializeField] private EnvironmentDepthManager depthManager;
//    [SerializeField] private TextMeshProUGUI debugText; // Optional: for on-headset display

//    private void Update()
//    {
//        if (tsdfManager == null)
//            tsdfManager = FindFirstObjectByType<TSDFVolumeManager>();

//        if (depthManager == null)
//            depthManager = FindFirstObjectByType<EnvironmentDepthManager>();

//        string debugInfo = GenerateDebugInfo();

//        // Print to console every 2 seconds
//        if (Time.frameCount % 120 == 0)
//        {
//            Debug.Log("=== TSDF Debug Info ===\n" + debugInfo);
//        }

//        // Display on headset if TextMeshPro assigned
//        if (debugText != null)
//        {
//            debugText.text = debugInfo;
//        }
//    }

//    private string GenerateDebugInfo()
//    {
//        string info = "";

//        // Check Depth Manager
//        info += "=== ENVIRONMENT DEPTH ===\n";
//        if (depthManager == null)
//        {
//            info += "❌ EnvironmentDepthManager: NOT FOUND\n";
//        }
//        else
//        {
//            info += $"✓ EnvironmentDepthManager: Found\n";
//            info += $"  Enabled: {depthManager.enabled}\n";
//            info += $"  Depth Available: {depthManager.IsDepthAvailable}\n";
//            info += $"  Occlusion Mode: {depthManager.OcclusionShadersMode}\n";
//        }

//        // Check depth texture
//        Texture depthTex = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
//        info += $"  Global Depth Texture: {(depthTex != null ? "✓ Present" : "❌ NULL")}\n";

//        info += "\n=== TSDF SYSTEM ===\n";
//        if (tsdfManager == null)
//        {
//            info += "❌ TSDFVolumeManager: NOT FOUND\n";
//        }
//        else
//        {
//            info += $"✓ TSDFVolumeManager: Found\n";
//            info += $"  Enabled: {tsdfManager.enabled}\n";
//            info += $"  Volume: {(tsdfManager.GetTSDFVolume() != null ? "✓ Created" : "❌ NULL")}\n";
//            info += $"  Resolution: {tsdfManager.GetResolution()}\n";
//            info += $"  Size: {tsdfManager.GetVolumeSize()}\n";
//        }

//        // Check cameras
//        info += "\n=== CAMERAS ===\n";
//        Camera mainCam = Camera.main;
//        info += $"Main Camera: {(mainCam != null ? mainCam.name : "NULL")}\n";

//        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
//        if (rig != null)
//        {
//            info += $"OVRCameraRig: {rig.name}\n";
//            info += $"  Center Eye: {rig.centerEyeAnchor.name}\n";
//        }
//        else
//        {
//            info += "❌ OVRCameraRig: NOT FOUND\n";
//        }

//        // Check for rendering issues
//        info += "\n=== RENDERING ===\n";
//        info += $"Active Cameras: {Camera.allCamerasCount}\n";

//        foreach (Camera cam in Camera.allCameras)
//        {
//            info += $"  - {cam.name}: depth={cam.depth}, cullingMask={cam.cullingMask}\n";
//        }

//        return info;
//    }
//}