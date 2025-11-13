using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// This script shoots a specified prefab from designated spawn points on the left and right controllers
/// when the index triggers are pressed.
/// </summary>
public class buttontest : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("The prefab to be instantiated and shot.")]
    public GameObject projectilePrefab;

    [Tooltip("The speed at which the projectile will be fired.")]
    public float projectileSpeed = 20f;


    [Header("Controller References")]
    [Tooltip("The transform representing the spawn point for the left hand's projectile.")]
    public Transform leftHandSpawnPoint;

    [Tooltip("The transform representing the spawn point for the right hand's projectile.")]
    public Transform rightHandSpawnPoint;

    void Update()
    {
        // --- Check for Left Controller Trigger Press ---
        // OVRInput.GetDown checks for the initial press of the button.
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            ShootPrefab(leftHandSpawnPoint);
        }

        // --- Check for Right Controller Trigger Press ---
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            ShootPrefab(rightHandSpawnPoint);
        }
    }

    /// <summary>
    /// Instantiates and fires the projectile from a given spawn point.
    /// </summary>
    /// <param name="spawnPoint">The transform from which to fire.</param>
    private void ShootPrefab(Transform spawnPoint)
    {
        // Safety check: Do nothing if the prefab or spawn point isn't set.
        if (projectilePrefab == null || spawnPoint == null)
        {
            Debug.LogError("Projectile Prefab or Spawn Point is not set in the Inspector!");
            return;
        }

        // --- THE MAGIC IS HERE ---

        // 1. Define the rotation we need to apply to fix our model's orientation.
        // Quaternion.Euler creates a rotation from the familiar X, Y, Z degree values.
        Quaternion rotationOffset = Quaternion.Euler(90, 0, 0);

        // 2. Combine the spawn point's rotation with our offset.
        // The order matters! Controller rotation first, then our local adjustment.
        Quaternion finalRotation = spawnPoint.rotation * rotationOffset;

        // 3. Instantiate the projectile using this new, corrected rotation.
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPoint.position, finalRotation);


        // Get the Rigidbody component from the newly created projectile.
        Rigidbody rb = newProjectile.GetComponent<Rigidbody>();

        // If the projectile has a Rigidbody, apply velocity to make it move forward.
        if (rb != null)
        {
            // spawnPoint.forward gives us the blue-axis "forward" direction of the spawn point.
            rb.linearVelocity = spawnPoint.forward * projectileSpeed;
        }
        else
        {
            Debug.LogWarning("The projectile prefab is missing a Rigidbody component. It won't move on its own.");
        }
    }
}