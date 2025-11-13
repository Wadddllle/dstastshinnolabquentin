using UnityEngine;
using System.IO;
using System.Text;

public class MeshExporter : MonoBehaviour
{
    public MeshFilter targetMeshFilter;

    [ContextMenu("Export Mesh To OBJ")]
    public void ExportMesh()
    {
        if (targetMeshFilter == null)
        {
            Debug.LogError("Target Mesh Filter is not assigned!");
            return;
        }

        Mesh mesh = targetMeshFilter.mesh;
        if (mesh == null)
        {
            Debug.LogError("The assigned Mesh Filter has no mesh!");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // Write vertices
        foreach (Vector3 v in mesh.vertices)
        {
            // OBJ format uses a different coordinate system, so we flip the Z-axis
            sb.AppendLine($"v {v.x} {v.y} {-v.z}");
        }

        // Write normals
        foreach (Vector3 n in mesh.normals)
        {
            // Flip Z-axis for normals as well
            sb.AppendLine($"vn {n.x} {n.y} {-n.z}");
        }

        // Write UVs (if they exist)
        foreach (Vector2 uv in mesh.uv)
        {
            sb.AppendLine($"vt {uv.x} {uv.y}");
        }

        // Write triangles (faces)
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            // OBJ indices are 1-based, not 0-based!
            int i1 = mesh.triangles[i] + 1;
            int i2 = mesh.triangles[i + 1] + 1;
            int i3 = mesh.triangles[i + 2] + 1;

            // In OBJ, the format for a face vertex is: vertex_index/uv_index/normal_index
            // We use the same index for all three parts here.
            sb.AppendLine($"f {i3}/{i3}/{i3} {i2}/{i2}/{i2} {i1}/{i1}/{i1}");
        }

        string filePath = Path.Combine(Application.dataPath, "../exported_mesh.obj");
        File.WriteAllText(filePath, sb.ToString());

        Debug.Log($"Mesh exported successfully to: {filePath}");
    }
}