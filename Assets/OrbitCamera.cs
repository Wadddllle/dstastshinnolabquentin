using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public Transform player;        // player to orbit around
    public Vector3 cam2dOffset = new Vector3(5, 0, -5);
    public float height = 7f;       // camera height offset
    public float lookSensitivity = 1f;
    public float pitchMin = 20f;    // vertical clamp
    public float pitchMax = 60f;
    
    private float yaw;   // horizontal rotation
    private float pitch; // vertical rotation
    private PlayerInputActions input;
    private Vector2 lookInput;

    void Awake()
    {
        input = new PlayerInputActions();
    }
    void OnEnable()
    {
        input.Enable();
    }
    void OnDisable()
    {
        input.Disable();
    }

    void Update()
    {
        lookInput = input.Player.Look.ReadValue<Vector2>();
    }

    void LateUpdate()
    {
        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPosition = player.position + Vector3.up * height + rotation * cam2dOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 2f); ;
        transform.LookAt(player.position + Vector3.up * 1.6f);
    }
}
