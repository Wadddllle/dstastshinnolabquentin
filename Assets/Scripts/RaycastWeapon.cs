using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class RaycastWeapon : MonoBehaviour
{
    [Header("Settings")]
    public Transform muzzlePoint;
    public LayerMask hitLayers;
    public float maxRange = 100f;

    [Header("Visual Debugging")]
    public bool showImpactSphere = true; // Spawns a red ball where you hit
    public float tracerDuration = 0.2f;  // Increased from 0.05 so you can actually see it
    public float tracerWidth = 0.02f;    // 2cm wide beam

    private LineRenderer _lr;

    void Start()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.positionCount = 2;
        _lr.enabled = false;

        // --- VISUAL FIX 1: Force a visible material ---
        // If you forgot to assign a material, this prevents it from being invisible or pink
        if (_lr.material == null || _lr.material.name.StartsWith("Default-Line"))
        {
            _lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        // --- VISUAL FIX 2: Force color and width ---
        _lr.startColor = Color.yellow; // Make it bright Yellow
        _lr.endColor = Color.red;
        _lr.startWidth = tracerWidth;
        _lr.endWidth = tracerWidth;
    }

    public void Fire()
    {
        if (muzzlePoint == null) return;

        // 1. Setup Start Point
        _lr.SetPosition(0, muzzlePoint.position);
        Vector3 endPoint = muzzlePoint.position + (muzzlePoint.forward * maxRange);

        // 2. Raycast
        Ray ray = new Ray(muzzlePoint.position, muzzlePoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRange, hitLayers))
        {
            endPoint = hit.point; // Update visual end point to where we hit

            // --- VISUAL FIX 3: Debug Impact Sphere ---
            if (showImpactSphere)
            {
                // Create a temporary red ball at the hit point
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = hit.point;
                sphere.transform.localScale = Vector3.one * 0.1f; // 10cm ball
                sphere.GetComponent<Renderer>().material.color = Color.red;
                Destroy(sphere.GetComponent<Collider>()); // No physics
                Destroy(sphere, 3.0f); // Delete after 3 seconds
            }

            // Game Logic (Your existing hit logic)
            TargetBehavior target = hit.collider.GetComponentInParent<TargetBehavior>();
            if (target != null)
            {
                target.HitByBullet(hit.point);
            }
        }

        // 3. Show Tracer
        _lr.SetPosition(1, endPoint);
        StartCoroutine(ShowTracer());

        // Console check
        Debug.Log($"[Gun] Fired! Hit: {(hit.collider ? hit.collider.name : "Nothing")}");
    }

    private IEnumerator ShowTracer()
    {
        _lr.enabled = true;
        yield return new WaitForSeconds(tracerDuration); // Stay visible longer
        _lr.enabled = false;
    }
}