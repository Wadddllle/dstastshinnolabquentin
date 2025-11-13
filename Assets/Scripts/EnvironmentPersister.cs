using Meta.XR.BuildingBlocks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentPersister : MonoBehaviour
{
    [Tooltip("The name of the clean scene you want to load the environment into.")]
    public string destinationSceneName = "AAR_Testbed_Clean";

    [ContextMenu("Execute Persist and Load (SAFE CLONE)")]
    public void PersistAndLoad()
    {
        // 1. Find the original, live SDK-managed room mesh object.
        RoomMeshAnchor originalRoomMesh = FindFirstObjectByType<RoomMeshAnchor>();
        if (originalRoomMesh == null)
        {
            Debug.LogError("Could not find a RoomMeshAnchor! Has the room scan completed?");
            return;
        }

        // --- This is the key part: The Cloning Process ---

        // 2. Create a new, empty GameObject to be our clean container.
        GameObject clonedEnvironment = new GameObject("[ClonedAAR_Environment]");

        // 3. Copy the mesh data.
        MeshFilter originalMeshFilter = originalRoomMesh.GetComponent<MeshFilter>();
        if (originalMeshFilter != null)
        {
            MeshFilter clonedMeshFilter = clonedEnvironment.AddComponent<MeshFilter>();
            clonedMeshFilter.sharedMesh = originalMeshFilter.sharedMesh; // Direct copy of the mesh data

            // 4. Copy the visual renderer and its materials.
            MeshRenderer originalRenderer = originalRoomMesh.GetComponent<MeshRenderer>();
            MeshRenderer clonedRenderer = clonedEnvironment.AddComponent<MeshRenderer>();
            clonedRenderer.sharedMaterials = originalRenderer.sharedMaterials; // Copy all materials

            // 5. CRUCIAL: Add a NEW MeshCollider. This makes the cloned mesh solid for raycasts.
            // Do NOT copy the old collider component, as it might have SDK dependencies.
            MeshCollider clonedCollider = clonedEnvironment.AddComponent<MeshCollider>();
            clonedCollider.sharedMesh = originalMeshFilter.sharedMesh; // Use the same mesh data for physics
        }

        // Set the clone's position and rotation to match the original
        clonedEnvironment.transform.position = originalRoomMesh.transform.position;
        clonedEnvironment.transform.rotation = originalRoomMesh.transform.rotation;

        Debug.Log($"Successfully created a clean clone of '{originalRoomMesh.name}'.");

        // 6. Now, persist the CLONE, not the original.
        DontDestroyOnLoad(clonedEnvironment);

        // --- The original SDK object is now left alone to be destroyed cleanly ---

        // 7. Load the new scene.
        Debug.Log($"Loading destination scene: {destinationSceneName}");
        SceneManager.LoadScene(destinationSceneName);
    }
}