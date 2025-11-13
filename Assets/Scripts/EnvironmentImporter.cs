using UnityEngine;
using System.IO;
using Dummiesman; // The required namespace for the asset

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class EnvironmentImporter : MonoBehaviour
{
    void Start()
    {
        LoadEnvironment();
    }

    [ContextMenu("Load Environment Snapshot")]
    public void LoadEnvironment()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "environment_transform.json");
        string objPath = Path.Combine(Application.persistentDataPath, "environment_mesh.obj");

        if (!File.Exists(jsonPath) || !File.Exists(objPath))
        {
            Debug.LogError("Cannot find environment snapshot files in " + Application.persistentDataPath);
            return;
        }

        // --- Part 1: Load the Transform Data ---
        string json = File.ReadAllText(jsonPath);
        TransformData transformData = JsonUtility.FromJson<TransformData>(json);


        // --- Part 2: Load the OBJ Mesh Data using the Dummiesman Asset ---

        // 1. Instantiate the loader from the Dummiesman package
        OBJLoader objLoader = new OBJLoader();

        // 2. Load the OBJ file from the path. This creates a NEW GameObject in your scene.
        GameObject tempLoadedObject = objLoader.Load(objPath);

        // 3. Extract the Mesh data we need from the MeshFilter on the object it created.
        Mesh loadedMesh = tempLoadedObject.GetComponentInChildren<MeshFilter>().sharedMesh;

        // 4. Extract the materials from the new object's renderer.
        Material[] loadedMaterials = tempLoadedObject.GetComponentInChildren<Renderer>().sharedMaterials;

        // 5. Destroy the temporary GameObject the loader created to avoid having a duplicate.
        Destroy(tempLoadedObject);


        // --- Part 3: Apply Everything to this GameObject ---

        // Apply the mesh we extracted
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = loadedMesh;

        // Apply the materials we extracted
        GetComponent<MeshRenderer>().materials = loadedMaterials;

        // Add a collider so we can raycast against it
        if (gameObject.GetComponent<MeshCollider>() == null)
        {
            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = loadedMesh;
        }

        // Apply the saved transform AFTER setting the mesh
        transform.position = transformData.position;
        transform.rotation = transformData.rotation;
        transform.localScale = transformData.localScale;

        Debug.Log("Environment snapshot loaded and reconstructed successfully.");
    }
}