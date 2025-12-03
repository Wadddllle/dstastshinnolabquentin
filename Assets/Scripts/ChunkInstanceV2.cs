using UnityEngine;

// This component is now just a simple container for the mesh and collider.
// It has no async logic and performs no heavy work.
public class ChunkInstanceV2 : MonoBehaviour
{
    private Vector3Int _coordinate;
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private MeshRenderer _meshRenderer;

    // A much simpler initialization. It just sets up components.
    public void Initialize(Vector3Int coordinate, Material material)
    {
        _coordinate = coordinate;
        gameObject.name = $"Chunk_{_coordinate.x}_{_coordinate.y}_{_coordinate.z}";

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.material = material;
        _meshCollider = gameObject.AddComponent<MeshCollider>();
    }

    // A public method for the Processor to call when a mesh is ready.
    public void ApplyMesh(Mesh newMesh)
    {
        if (newMesh == null)
        {
            // If the new mesh is null, it means the chunk is empty.
            // Clear the old one and disable the components.
            if (_meshFilter.mesh != null) Destroy(_meshFilter.mesh);
            _meshFilter.mesh = null;
            _meshCollider.sharedMesh = null;
            _meshRenderer.enabled = false;
            return;
        }

        // Destroy the old mesh to prevent memory leaks.
        if (_meshFilter.mesh != null)
        {
            Destroy(_meshFilter.mesh);
        }

        _meshFilter.mesh = newMesh;
        _meshCollider.sharedMesh = newMesh;
        _meshRenderer.enabled = true; // Make sure it's visible.
    }

    public void SetRendererVisibility(bool isVisible)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = isVisible;
        }
    }
}