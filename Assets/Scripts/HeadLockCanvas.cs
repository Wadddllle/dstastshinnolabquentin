using UnityEngine;

public class HeadLockCanvas : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetCamera; // Drag CenterEyeAnchor here
    public float distanceFromCamera = 2.0f;
    public Vector3 offsetRotation;

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Position the object in front of the camera
        transform.position = targetCamera.position + (targetCamera.forward * distanceFromCamera);

        // 2. Rotate the object to match the camera's rotation
        transform.rotation = targetCamera.rotation * Quaternion.Euler(offsetRotation);
    }
}