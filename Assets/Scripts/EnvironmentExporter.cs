using UnityEngine;
using System.IO;
using System.Text;
using Meta.XR.BuildingBlocks; // We still need this to find the object

public class EnvironmentExporter : MonoBehaviour
{
    // We will find the target automatically
    private MeshFilter _targetMeshFilter;

    [ContextMenu("Export Environment Snapshot")]
    public void ExportEnvironment()
    {
        // --- Part 1: Find the target and save its Transform ---

        RoomMeshAnchor roomMesh = FindFirstObjectByType<RoomMeshAnchor>();
        if (roomMesh == null)
        {
            Debug.LogError("Could not find RoomMeshAnchor. Has the room scan completed?");
            return;
        }

        _targetMeshFilter = roomMesh.GetComponent<MeshFilter>();
        if (_targetMeshFilter == null || _targetMeshFilter.sharedMesh == null)
        {
            Debug.LogError("Found RoomMeshAnchor but it has no mesh data!");
            return;
        }

        // Create the data object
        TransformData transformData = new TransformData
        {
            position = roomMesh.transform.position,
            rotation = roomMesh.transform.rotation,
            localScale = roomMesh.transform.localScale
        };

        // Convert to JSON and save
        string json = JsonUtility.ToJson(transformData, true); // 'true' for pretty print
        string jsonPath = Path.Combine(Application.persistentDataPath, "environment_transform.json");
        File.WriteAllText(jsonPath, json);
        Debug.Log($"Transform data saved to: {jsonPath}");


        // --- Part 2: Your OBJ Export Logic ---
        ExportMeshToOBJ(jsonPath); // Pass the path for a better debug message
    }

    private void ExportMeshToOBJ(string jsonPath)
    {
        Mesh mesh = _targetMeshFilter.sharedMesh; // Use sharedMesh to avoid instantiating
        StringBuilder sb = new StringBuilder();

        // Write vertices (we don't need to flip Z for this workflow)
        foreach (Vector3 v in mesh.vertices) sb.AppendLine($"v {v.x} {v.y} {v.z}");
        // Write normals
        foreach (Vector3 n in mesh.normals) sb.AppendLine($"vn {n.x} {n.y} {n.z}");
        // Write UVs
        foreach (Vector2 uv in mesh.uv) sb.AppendLine($"vt {uv.x} {uv.y}");

        // Write triangles (faces)
        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] triangles = mesh.GetTriangles(submesh);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // OBJ indices are 1-based
                int i1 = triangles[i] + 1;
                int i2 = triangles[i + 1] + 1;
                int i3 = triangles[i + 2] + 1;
                sb.AppendLine($"f {i3}/{i3}/{i3} {i2}/{i2}/{i2} {i1}/{i1}/{i1}");
            }
        }

        string objPath = Path.Combine(Application.persistentDataPath, "environment_mesh.obj");
        File.WriteAllText(objPath, sb.ToString());
        Debug.Log($"Mesh and Transform exported successfully. Data is in: {Application.persistentDataPath}");
    }
}