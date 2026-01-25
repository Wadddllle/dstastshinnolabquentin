using UnityEngine;
using UnityEngine.UI; // Required for UI Text
using System.Collections;
using TMPro;

public class ForceTubeDebugger : MonoBehaviour
{
    [Header("UI (Optional)")]
    [Tooltip("Drag a UI Text object here to see logs inside the headset")]
    public TextMeshProUGUI debugOutput;

    private string logBuffer = "";

    IEnumerator Start()
    {
        Log("--- STARTING FORCETUBE DEBUG ---");

        // 1. Check Permissions (Sanity Check)
        bool btConnect = UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT");
        Log($"Permission BT_CONNECT: {btConnect}");

        // 2. Clear previous sessions
        Log("Step 1: Force Disconnect...");
        ForceTubeVRInterface.DisconnectAll();
        yield return new WaitForSeconds(0.5f);

        // 3. Initialize
        Log("Step 2: InitAsync calling...");
        ForceTubeVRInterface.InitAsync(true); // true = pistol first
        yield return new WaitForSeconds(1.0f);

        // 4. Loop check
        StartCoroutine(StatusLoop());
    }

    IEnumerator StatusLoop()
    {
        while (true)
        {
            // Get list of connected guns (Returns JSON string)
            string connectedJson = ForceTubeVRInterface.ListConnectedForceTube();

            // Get list of active channels (Returns JSON string)
            string channelsJson = ForceTubeVRInterface.ListChannels();

            // Get battery (only works if connected)
            byte battery = ForceTubeVRInterface.GetBatteryLevel();

            // Format the log
            string statusMsg = $"\n--- STATUS CHECK ({Time.time:F1}) ---\n" +
                               $"Raw Connected JSON: {connectedJson}\n" +
                               $"Raw Channels JSON: {channelsJson}\n" +
                               $"Battery Read: {battery}%\n" +
                               $"-----------------------------";

            // Print to Unity Console (Logcat)
            Debug.Log(statusMsg);

            // Print to VR Screen (if UI Text is attached)
            if (debugOutput != null)
            {
                // Keep only last 500 chars to avoid screen clutter
                if (logBuffer.Length > 1000) logBuffer = "";
                debugOutput.text = statusMsg + "\n" + logBuffer;
            }

            // Blink the LED on the gun if we think we are connected
            if (!string.IsNullOrEmpty(connectedJson) && connectedJson != "[]")
            {
                // Rumble very briefly to prove connection physically
                ForceTubeVRInterface.Rumble(100, 50f, ForceTubeVRChannel.all);
            }

            yield return new WaitForSeconds(2.0f);
        }
    }

    // Helper to log to both Console and Screen
    void Log(string msg)
    {
        Debug.Log(msg);
        logBuffer = msg + "\n" + logBuffer;
        if (debugOutput != null) debugOutput.text = logBuffer;
    }
}