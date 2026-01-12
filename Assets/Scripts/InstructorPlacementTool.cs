using UnityEngine;

public class InstructorPlacementTool : MonoBehaviour
{
    [Header("Settings")]
    public GameObject prefabToPlace; // Your target/enemy prefab
    public Transform rightHandSpawnPoint;

    // We only update if this component is enabled by the State!
    void Update()
    {
        // Check for Right Trigger Press
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            PlaceObject(rightHandSpawnPoint);
        }
    }

    private void PlaceObject(Transform spawnPoint)
    {
        if (prefabToPlace == null) return;

        // 1. Instantiate the object
        // (Using simple spawning at hand position for accurate placement, 
        // but you can keep your shooting logic if you prefer)
        GameObject newObj = Instantiate(prefabToPlace, spawnPoint.position, Quaternion.identity);

        // 2. IMPORTANT: Save this location to the SessionData!
        // This ensures the AAR knows where the enemies were.
        //Pose enemyPose = new Pose(spawnPoint.position, Quaternion.identity);
        //AppManager.Instance.currentSession.enemySpawnLocations.Add(enemyPose);

        Debug.Log($"Placed Enemy at: {spawnPoint.position}");
    }
}