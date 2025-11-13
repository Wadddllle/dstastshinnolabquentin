using UnityEngine;

/// <summary>
/// Quick diagnostic tool to check mesh visibility issues
/// </summary>
public class MeshVisibilityChecker : MonoBehaviour
{
    private void Update()
    {
        // Press V to run visibility check
        if (Time.frameCount % 300 == 0)
        {
            CheckAllMeshes();
        }
    }

    private void CheckAllMeshes()
    {
        Debug.Log("=== MESH VISIBILITY CHECK ===");

        // Find all MeshRenderers in scene
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        Debug.Log($"Found {renderers.Length} MeshRenderers in scene");

        foreach (var renderer in renderers)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();

            string status = $"GameObject: {renderer.gameObject.name}\n";
            status += $"  Position: {renderer.transform.position}\n";
            status += $"  Active: {renderer.gameObject.activeInHierarchy}\n";
            status += $"  Renderer Enabled: {renderer.enabled}\n";
            status += $"  Material: {(renderer.material != null ? renderer.material.name : "NULL")}\n";
            status += $"  Has MeshFilter: {(filter != null)}\n";

            if (filter != null)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh != null)
                {
                    status += $"  Mesh: {mesh.name}\n";
                    status += $"  Vertices: {mesh.vertexCount}\n";
                    status += $"  Triangles: {mesh.triangles.Length / 3}\n";
                    status += $"  Bounds: {mesh.bounds}\n";
                }
                else
                {
                    status += $"  Mesh: NULL\n";
                }
            }

            Debug.Log(status);
        }

        // Specifically look for TSDF mesh
        GameObject tsdfMesh = GameObject.Find("TSDF_Mesh");
        if (tsdfMesh != null)
        {
            Debug.Log($"✓ TSDF_Mesh found at: {tsdfMesh.transform.position}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(tsdfMesh.layer)}");
            Debug.Log($"  Parent: {(tsdfMesh.transform.parent != null ? tsdfMesh.transform.parent.name : "None")}");
        }
        else
        {
            Debug.LogWarning("❌ TSDF_Mesh GameObject not found!");
        }

        Debug.Log("=== END VISIBILITY CHECK ===");
    }
}