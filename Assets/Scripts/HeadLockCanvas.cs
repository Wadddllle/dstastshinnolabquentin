using UnityEngine;

public class HeadLockCanvas : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetCamera; // Drag CenterEyeAnchor here
    public float distanceFromCamera_default = 2.0f;
    public Vector3 offsetRotation;

    public float maxOffset_H = 1f;
    public float maxOffset_V = 1f;
    public float maxOffset_F = 1f;
    public float scrollSpeed = 1f;

    float horizontalOffset = 0f;
    float verticalOffset = 0f;
    float distanceFromCamera;
    
    void Start()
    {
        distanceFromCamera = distanceFromCamera_default;
    }

    void Update()
    {
        Vector2 leftJoystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick,OVRInput.Controller.LTouch);
        Vector2 rightJoystick = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick, OVRInput.Controller.RTouch);
        float horizontalInput = leftJoystick.x;
        float verticalInput = leftJoystick.y;
        float forwardInput = rightJoystick.y;

        horizontalOffset += horizontalInput * scrollSpeed * Time.deltaTime;
        horizontalOffset = Mathf.Clamp(horizontalOffset, -maxOffset_H, maxOffset_H);

        verticalOffset += verticalInput * scrollSpeed * Time.deltaTime;
        verticalOffset = Mathf.Clamp(verticalOffset, -maxOffset_V, maxOffset_V);

        /*distanceFromCamera += forwardInput * scrollSpeed * Time.deltaTime;
        distanceFromCamera = Mathf.Clamp(distanceFromCamera, distanceFromCamera_default-maxOffset_F, distanceFromCamera_default+maxOffset_F);*/
    }
    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 1. Position the object in front of the camera
        transform.position = targetCamera.position + (targetCamera.forward * distanceFromCamera) + targetCamera.right * horizontalOffset + targetCamera.up * verticalOffset; //positive offset to the right, forward and up

        // 2. Rotate the object to match the camera's rotation
        transform.rotation = targetCamera.rotation * Quaternion.Euler(offsetRotation);
    }
}