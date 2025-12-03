using UnityEngine;

public class VisionScanner : MonoBehaviour
{
    [Header("Settings")]
    public float Range = 10f; // How far you can see
    public float FOV = 90f;   // Horizontal field of view
    public int RaysPerFrame = 20; // How many rays to cast per update

    [Header("Layers")]
    public LayerMask ObstacleMask; // Set this to "Default" or whatever your Mesh is on

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    void FixedUpdate()
    {
        Scan();
    }

    void Scan()
    {
        Vector3 origin = transform.position;

        for (int i = 0; i < RaysPerFrame; i++)
        {
            // 1. Pick a random angle within the FOV (Creates a noisy "cone")
            float angle = Random.Range(-FOV / 2f, FOV / 2f);
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * transform.forward;

            // 2. Cast Ray
            RaycastHit hit;
            float distanceToPaint = Range;

            // Did we hit a wall/mesh?
            if (Physics.Raycast(origin, dir, out hit, Range, ObstacleMask))
            {
                distanceToPaint = hit.distance;
                // Optional: Visualize the hit for debug
                Debug.DrawLine(origin, hit.point, Color.red, 0.1f);
            }
            else
            {
                Debug.DrawLine(origin, origin + dir * Range, Color.green, 0.1f);
            }

            // 3. "Walk" along the ray and paint the floor
            // We step every 0.1m (10cm) to match the grid size
            for (float d = 0; d < distanceToPaint; d += 0.1f)
            {
                Vector3 pointAlongRay = origin + (dir * d);
                // Paint this specific spot
                TacticalGrid.Instance.MarkSeen(pointAlongRay);
            }
        }
    }
}