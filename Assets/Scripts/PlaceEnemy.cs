using UnityEngine;
using Unity.VisualScripting;

/// <summary>
/// This script shoots a specified prefab from designated spawn points on the left and right controllers
/// when the A/X buttons are pressed, but only when controllers are actively in use.
/// </summary>
public class PlaceEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("The prefab to be instantiated and shot.")]
    public GameObject projectilePrefab;

    [Header("Controller References")]
    [Tooltip("The transform representing the spawn point for the left hand's projectile.")]
    public Transform leftHandSpawnPoint;

    [Tooltip("The transform representing the spawn point for the right hand's projectile.")]
    public Transform rightHandSpawnPoint;

    void Update()
    {
        if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch)
        {
            // --- Check for Left Controller X Button Press (OVRInput.Button.One) ---
            // This will only be checked when the physical LTouch controller is active.
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            {
                ShootPrefab(leftHandSpawnPoint);
            }
        }

        if (OVRInput.GetActiveController() == OVRInput.Controller.RTouch)
        {
            // --- Check for Right Controller A Button Press (OVRInput.Button.One) ---
            // This will only be checked when the physical RTouch controller is active.
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                ShootPrefab(rightHandSpawnPoint);
            }
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
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
    }
}