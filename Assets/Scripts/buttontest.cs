using Unity.VisualScripting;
using UnityEngine;

public class Buttontest : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
    public float bulletLifeTime = 5.0f;

    [Header("Effects")]
    public AudioClip gunshotSound;
    [Tooltip("Drag the AudioSource component here")]
    public AudioSource weaponAudioSource;
    public ForceTubeController haptics;

    [Header("Right Hand (Weapon)")]
    public Transform rightMuzzlePoint; // The tip of the gun barrel
    public ParticleSystem rightMuzzleFlash; // The particle system on the barrel

    [Header("Left Hand (Optional/Dual Wield)")]
    public Transform leftMuzzlePoint;
    public ParticleSystem leftMuzzleFlash;

    public Transform shotSource;

    [SerializeField] private bool stillSilent = true;


    void Update()
    {
        // Right Controller (Primary Trigger)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            FireWeapon(rightMuzzlePoint, rightMuzzleFlash);
            shotSource = rightMuzzlePoint;
        }

        // Left Controller (Optional)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch))
        {
            FireWeapon(leftMuzzlePoint, leftMuzzleFlash);
            shotSource = leftMuzzlePoint;
        }
    }

    private void FireWeapon(Transform spawnPoint, ParticleSystem muzzleFlash)
    {
        if (projectilePrefab == null || spawnPoint == null) 
            return;
            
        if (haptics != null) haptics.FireHaptic();
        // --- 1. Visual & Audio Effects ---
        // Play Muzzle Flash
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop(); // Reset if already playing
            muzzleFlash.Play();
        }

        // Play Sound
        if (weaponAudioSource != null && gunshotSound != null)
        {
            weaponAudioSource.PlayOneShot(gunshotSound);
        }

        // --- 2. Log the Shot Event ---
        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("SHOT", $"Fired from {spawnPoint.name}", spawnPoint.position);
        }

        // --- 3. Check if player has already shot before ---
        if (stillSilent == true)
        {
            EnemyAI[] allEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (var enemy in allEnemies)
                enemy.AlertByGunshot();         //all enemies will be alerted to move towards player upon first shot out

            HostageAI[] allHostages = FindObjectsByType<HostageAI>(FindObjectsSortMode.None);
            foreach (var hostage in allHostages)
                hostage.AlertByGunshot();       //all hostages will be alerted to crouch down upon first shot out

            stillSilent = false;
        }
        // --- 4. Create the Bullet ---
        // Adjust rotation by 90 degrees on X if your bullet model is lying flat
        Quaternion rotationOffset = Quaternion.Euler(90, 0, 0);
        Quaternion finalRotation = spawnPoint.rotation * rotationOffset;

        GameObject newProjectile = Instantiate(projectilePrefab, spawnPoint.position, finalRotation);

        // --- 5. Apply Velocity ---
        Rigidbody rb = newProjectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Note: Use rb.velocity if using Unity 2022 or older. 
            // rb.linearVelocity is for Unity 6000+ (Unity 6).
            rb.linearVelocity = spawnPoint.forward * projectileSpeed;
        }

        // --- 6. Cleanup ---
        Destroy(newProjectile, bulletLifeTime);
    }
}