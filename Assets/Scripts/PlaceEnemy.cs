using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// This script shoots a specified prefab from designated spawn points on the left and right controllers
/// when the index triggers are pressed.
/// </summary>
public class PlaceEnemy : MonoBehaviour
{
    [Header("Projectile Settings")]
    [Tooltip("The prefab to be instantiated and shot.")]
    public GameObject projectilePrefab;

    //[Tooltip("The speed at which the projectile will be fired.")]
    //public float projectileSpeed = 20f;


    [Header("Controller References")]
    [Tooltip("The transform representing the spawn point for the left hand's projectile.")]
    public Transform leftHandSpawnPoint;

    [Tooltip("The transform representing the spawn point for the right hand's projectile.")]
    public Transform rightHandSpawnPoint;

    void Update()
    {
        // --- Check for Left Controller Trigger Press ---
        // OVRInput.GetDown checks for the initial press of the button.
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            ShootPrefab(leftHandSpawnPoint);
        }

        // --- Check for Right Controller Trigger Press ---
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
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
        GameObject newProjectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
    }
}