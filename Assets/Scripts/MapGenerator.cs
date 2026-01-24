
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Anaglyph.XRTemplate;

public class MapGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int resolution = 1024;
    public float sliceHeight = 1.5f;
    public float floorBuffer = 0.2f;

    [Header("Output")]
    public string fileName = "Map_Snapshot.png";

    public CanvasLogic canvasLogic;

    // Internal
    private Material _whiteMaterial;

    [ContextMenu("Capture Map Snapshot")]
    public void CaptureSnapshot()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 1. Get Dependencies
        var mapper = EnvironmentMapper.Instance;
        if (mapper == null)
        {
            Debug.LogError("MapGenerator: EnvironmentMapper not found!");
            yield break;
        }

        // 2. Setup Dimensions
        float widthMeters = mapper.volume.width * mapper.metersPerVoxel;
        float depthMeters = mapper.volume.volumeDepth * mapper.metersPerVoxel;
        float maxDimension = Mathf.Max(widthMeters, depthMeters);
        float camOrthoSize = maxDimension / 2.0f;

        // 3. Create Temporary Material
        if (_whiteMaterial == null)
        {
            _whiteMaterial = new Material(Shader.Find("Unlit/Color"));
            _whiteMaterial.color = Color.white;
        }

        // 4. FIND CHUNKS AND FORCE VISIBILITY
        ChunkInstance[] chunks = FindObjectsByType<ChunkInstance>(FindObjectsSortMode.None);

        // Dictionary to store BOTH material and visibility state
        Dictionary<MeshRenderer, (Material mat, bool wasVisible)> originalStates = new Dictionary<MeshRenderer, (Material, bool)>();

        foreach (var chunk in chunks)
        {
            MeshRenderer r = chunk.GetComponent<MeshRenderer>();
            if (r != null)
            {
                // A. Save original state (Material AND Visibility)
                originalStates[r] = (r.sharedMaterial, r.enabled);

                // B. Force visible and apply white material
                r.enabled = true;
                r.material = _whiteMaterial;
            }
        }

        // Allow one frame for materials/visibility to update
        yield return new WaitForEndOfFrame();

        // 5. Setup Temporary Camera
        GameObject tempCamObj = new GameObject("MapShot_Cam");
        Camera cam = tempCamObj.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = camOrthoSize;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, sliceHeight, 0);
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = sliceHeight - floorBuffer;

        // 6. Render
        RenderTexture rt = new RenderTexture(resolution, resolution, 24);
        cam.targetTexture = rt;
        cam.Render();

        // 7. Save to Texture2D
        RenderTexture.active = rt;
        Texture2D mapTex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        mapTex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        mapTex.Apply();
        RenderTexture.active = null;

        // 8. Write to File
        byte[] bytes = mapTex.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(path, bytes);
        Debug.Log($"<color=green>Map Snapshot Saved to: {path}</color>");

        // 9. Cleanup
        cam.targetTexture = null;
        Destroy(rt);
        Destroy(tempCamObj);
        Destroy(mapTex);

        // 10. RESTORE ORIGINAL STATES
        foreach (var kvp in originalStates)
        {
            MeshRenderer r = kvp.Key;
            var state = kvp.Value;

            if (r != null)
            {
                r.material = state.mat;      // Restore Material
                r.enabled = state.wasVisible; // Restore Visibility (Hidden or Visible)
            }
        }
    }
}