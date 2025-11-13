using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;

[RequireComponent(typeof(TSDFVolumeManager))]
public class TSDFDebugInspector : MonoBehaviour
{
    private TSDFVolumeManager tsdfManager;

    void Start()
    {
        tsdfManager = GetComponent<TSDFVolumeManager>();
    }

    [ContextMenu("Inspect Entire TSDF Volume")]
    public void InspectVolume()
    {
        RenderTexture tsdfVolume = tsdfManager.GetTSDFVolume();
        if (tsdfVolume == null)
        {
            Debug.LogError("TSDF Volume is not available to inspect.");
            return;
        }

        Debug.Log("Starting GPU readback of ENTIRE TSDF volume... This may take a moment.");
        Vector3Int res = tsdfManager.GetResolution();

        // Use the explicit overload to define the full 3D region to read.
        // This guarantees we get the whole volume, not just the first slice.
        AsyncGPUReadback.Request(tsdfVolume, 0, 0, res.x, 0, res.y, 0, res.z, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("GPU readback failed.");
                return;
            }

            Debug.Log("GPU readback complete. Analyzing data...");
            NativeArray<Vector2> data = request.GetData<Vector2>();

            int invalidCount = 0;
            int firstInvalidIndex = -1;

            for (int i = 0; i < data.Length; i++)
            {
                Vector2 voxelData = data[i];
                float tsdf = voxelData.x;
                float weight = voxelData.y;

                if (float.IsNaN(tsdf) || float.IsInfinity(tsdf) || float.IsNaN(weight) || float.IsInfinity(weight))
                {
                    if (invalidCount == 0) firstInvalidIndex = i;
                    invalidCount++;
                }
            }

            if (invalidCount > 0)
            {
                int z = firstInvalidIndex / (res.x * res.y);
                int y = (firstInvalidIndex % (res.x * res.y)) / res.x;
                int x = firstInvalidIndex % res.x;

                Debug.LogError($"❌ CORRUPTION DETECTED! Found {invalidCount} invalid voxels.");
                Debug.LogError($"First invalid voxel at index {firstInvalidIndex} -> coordinates ({x}, {y}, {z})");
            }
            else
            {
                Debug.Log("✅ Analysis complete. No corruption found in the TSDF volume.");
            }
        });
    }
}