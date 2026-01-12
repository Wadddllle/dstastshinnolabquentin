using UnityEngine;

public class VisionScanner : MonoBehaviour
{
    [Header("Settings")]
    public float Range = 10f;
    public float FOV = 90f;
    public int RaysPerFrame = 20;

    [Header("Layers")]
    public LayerMask ObstacleMask;

    private Transform _transform;

    void Start()
    {
        _transform = transform; // Cache transform for performance
    }

    void FixedUpdate()
    {
        // Safety Check: Don't scan if the Grid isn't ready!
        if (TacticalGrid.Instance == null || !TacticalGrid.Instance.IsInitialized) return;

        Scan();
    }

    void Scan()
    {
        Vector3 origin = _transform.position;
        Vector3 fwd = _transform.forward;

        for (int i = 0; i < RaysPerFrame; i++)
        {
            // 1. Pick a random angle within the FOV
            float angle = Random.Range(-FOV / 2f, FOV / 2f);

            // Rotate around the Y axis relative to current forward
            Quaternion rot = Quaternion.Euler(0, angle, 0);
            Vector3 dir = rot * fwd;

            // 2. Cast Ray
            RaycastHit hit;
            float distanceToPaint = Range;

            if (Physics.Raycast(origin, dir, out hit, Range, ObstacleMask))
            {
                distanceToPaint = hit.distance;
                // Debug.DrawLine(origin, hit.point, Color.red, 0.05f); // Turn off for performance later
            }

            // 3. "Walk" along the ray and paint
            // Optimization: Get the CellSize from the grid so we don't over-paint
            float stepSize = TacticalGrid.Instance.CellSize;
            if (stepSize <= 0) stepSize = 0.1f; // Safety fallback

            for (float d = 0; d < distanceToPaint; d += stepSize)
            {
                Vector3 pointAlongRay = origin + (dir * d);
                TacticalGrid.Instance.MarkSeen(pointAlongRay);
            }

            // Also mark the final hit point if we hit a wall
            if (distanceToPaint < Range)
            {
                TacticalGrid.Instance.MarkSeen(origin + (dir * distanceToPaint));
            }
        }
    }
}