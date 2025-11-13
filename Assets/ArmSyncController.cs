using UnityEngine;

public class ArmSyncController : MonoBehaviour
{
    [Header("Left Arm")]
    public HingeJoint leftArmHinge;
    public Rigidbody leftArmRB;

    [Header("Right Arm")]
    public HingeJoint rightArmHinge;
    public Rigidbody rightArmRB;

    [Header("Control Settings")]
    [Tooltip("The base speed you set in your HingeJoint's motor.")]
    public float baseSpinSpeed = 500f;
    [Tooltip("How strongly the script nudges (or brakes) an arm.")]
    public float nudgeStrength = 10f;
    [Tooltip("Won't apply any correction unless the error is bigger than this.")]
    public float errorThreshold = 1.0f; // 1 degree deadzone

    [Header("NEW: Offsets")]
    [Tooltip("Adjust this to fix starting position differences for the LEFT arm.")]
    public float leftArmOffset = 0f;
    [Tooltip("Adjust this to fix starting position differences for the RIGHT arm.")]
    public float rightArmOffset = 0f;

    [Header("Debug")]
    [Tooltip("Log state to the console every physics frame. THIS IS VERY SPAMMY!")]
    public bool enableDebugLogs = true;

    private float masterTargetAngle = 0f;

    void FixedUpdate()
    {
        // 1. Advance the *master* clock's target angle
        masterTargetAngle += baseSpinSpeed * Time.fixedDeltaTime;
        masterTargetAngle = masterTargetAngle % 360f;

        // --- 2. Correct Left Arm ---
        if (leftArmHinge != null && leftArmRB != null)
        {
            // Calculate the *specific* target for this arm, including its offset
            float leftTarget = masterTargetAngle + leftArmOffset;

            float error = Mathf.DeltaAngle(leftArmHinge.angle, leftTarget);

            if (enableDebugLogs)
            {
                // Log now shows the *corrected* target
                Debug.Log($"LEFT: Target={leftTarget:F2}, Actual={leftArmHinge.angle:F2}, Error={error:F2}");
            }

            if (Mathf.Abs(error) > errorThreshold)
            {
                // This is the raw power (gas or brake)
                float scalarTorque = error * nudgeStrength;

                if (enableDebugLogs)
                {
                    // This log is now more useful
                    Debug.Log($"--- NUDGING LEFT (Scalar: {scalarTorque:F2}) ---");
                }

                leftArmRB.AddRelativeTorque(leftArmHinge.axis * scalarTorque, ForceMode.Acceleration);
            }
        }

        // --- 3. Correct Right Arm (identical logic) ---
        if (rightArmHinge != null && rightArmRB != null)
        {
            // Calculate the *specific* target for this arm
            float rightTarget = masterTargetAngle + rightArmOffset;

            float error = Mathf.DeltaAngle(rightArmHinge.angle, rightTarget);

            if (enableDebugLogs)
            {
                Debug.Log($"RIGHT: Target={rightTarget:F2}, Actual={rightArmHinge.angle:F2}, Error={error:F2}");
            }

            if (Mathf.Abs(error) > errorThreshold)
            {
                float scalarTorque = error * nudgeStrength;

                if (enableDebugLogs)
                {
                    Debug.Log($"--- NUDGING RIGHT (Scalar: {scalarTorque:F2}) ---");
                }

                rightArmRB.AddRelativeTorque(rightArmHinge.axis * scalarTorque, ForceMode.Acceleration);
            }
        }
    }
}