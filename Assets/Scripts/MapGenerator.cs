using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Anaglyph.XRTemplate; // Needed for EnvironmentMapper

public class MapGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int resolution = 1024;
    public float sliceHeight = 1.5f; // Meters from the floor (approx chest height)
    public float floorBuffer = 0.2f; // How far above the floor to stop seeing (prevents seeing the floor mesh)

    [Header("Output")]
    public string fileName = "Map_Snapshot.png";

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

        // 2. Setup Dimensions based on your Volume
        // Assuming the Volume is centered at (0,0,0) or the Mapper's position.
        // We calculate the physical size of the room in meters.
        float widthMeters = mapper.volume.width * mapper.metersPerVoxel;
        float depthMeters = mapper.volume.volumeDepth * mapper.metersPerVoxel; // Height in data is Depth in world (usually)

        // Ortho size is vertical half-height. We take the larger dimension to fit everything.
        float maxDimension = Mathf.Max(widthMeters, depthMeters);
        float camOrthoSize = maxDimension / 2.0f;

        // 3. Create Temporary Material (Unlit White)
        if (_whiteMaterial == null)
        {
            _whiteMaterial = new Material(Shader.Find("Unlit/Color"));
            _whiteMaterial.color = Color.white;
        }

        // 4. Find all Chunks and Swap Materials
        // We assume your chunks have "ChunkInstance" component
        ChunkInstance[] chunks = FindObjectsByType<ChunkInstance>(FindObjectsSortMode.None);
        Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();

        foreach (var chunk in chunks)
        {
            MeshRenderer r = chunk.GetComponent<MeshRenderer>();
            if (r != null)
            {
                originalMaterials[r] = r.sharedMaterial; // Save original
                r.material = _whiteMaterial;             // Apply white
            }
        }

        // Allow one frame for materials to update visually (crucial!)
        yield return new WaitForEndOfFrame();

        // 5. Setup Temporary Camera
        GameObject tempCamObj = new GameObject("MapShot_Cam");
        Camera cam = tempCamObj.AddComponent<Camera>();

        cam.orthographic = true;
        cam.orthographicSize = camOrthoSize;
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // POS: Center of room, at Slice Height.
        // NOTE: We assume X/Z center is 0,0. Adjust if your Mapper has an offset.
        cam.transform.position = new Vector3(0, sliceHeight, 0);

        // ROT: Looking straight down
        cam.transform.rotation = Quaternion.Euler(90, 0, 0);

        // CLIP PLANES: The Magic "Slice" Logic
        // Near: 0 (Starts at camera)
        // Far:  Distance to floor - buffer. 
        // Example: If Cam is at 1.5m, and Floor is at 0m. Distance is 1.5m.
        // We want to stop 0.2m before the floor so we don't see the floor mesh, just walls.
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = sliceHeight - floorBuffer;

        // Culling: Only render the layer your Chunks are on! 
        // (If Chunks are Default, this renders everything Default)
        // cam.cullingMask = ...; // Optional: Set this if you have a specific layer

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

        // Restore Materials
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
    }
}