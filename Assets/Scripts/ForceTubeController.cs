using UnityEngine;
using System.Collections;

public class ForceTubeController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If true, prioritizes Pistols for channel assignment.")]
    public bool usePistolsFirst = false;

    [Header("Haptic Config")]
    [Range(0, 255)] public byte defaultKickPower = 200;
    [Range(0, 255)] public byte defaultRumblePower = 100;
    [Range(0, 500f)] public float defaultRumbleDuration = 200f; // in milliseconds

    void Start()
    {
        // Initialize the connection asynchronously when the game starts.
        // This dispatches devices to channels (Rifle, Pistol1, Pistol2, Vest, etc.)
        ForceTubeVRInterface.InitAsync(usePistolsFirst); //
    }

    /// <summary>
    /// Call this method when your gun fires a single shot.
    /// </summary>
    public void FireHaptic()
    {
        // Sends a combined Kick and Rumble request to the Rifle channel by default
        ForceTubeVRInterface.Shoot(defaultKickPower, defaultRumblePower, defaultRumbleDuration, ForceTubeVRChannel.rifle); //
    }

    /// <summary>
    /// Call this for automatic weapons to prevent motor locking.
    /// Calculates the optimal kick power based on fire rate.
    /// </summary>
    /// <param name="fireRateSeconds">Time between shots in seconds</param>
    public void FireAutoHaptic(float fireRateSeconds)
    {
        // Calculate max safe power for this fire rate
        byte safePower = ForceTubeVRInterface.TempoToKickPower(fireRateSeconds); //

        // Trigger the shot with the calculated safe power
        ForceTubeVRInterface.Shoot(safePower, defaultRumblePower, defaultRumbleDuration, ForceTubeVRChannel.rifle); //
    }

    /// <summary>
    /// Call this for sustained effects (e.g., charging a weapon, environmental damage).
    /// </summary>
    public void RumbleOnly()
    {
        ForceTubeVRInterface.Rumble(defaultRumblePower, defaultRumbleDuration, ForceTubeVRChannel.rifle); //
    }

    // Optional: Add a UI button to trigger this if automatic pairing fails
    public void OpenBluetoothSettings()
    {
        ForceTubeVRInterface.OpenBluetoothSettings(); //
    }

    private void OnApplicationQuit()
    {
        // Clean up connections when the app closes
        ForceTubeVRInterface.DisconnectAll(); //
    }
}