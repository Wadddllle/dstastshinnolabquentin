using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class InstructorPlacementTool : MonoBehaviour
{
    [Header("Controller Settings")]
    public Transform controllerAnchor;
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Prefab Settings")]
    public GameObject prefabToPlace;
    public float verticalOffset = 0.85f; // Set this to 0.85 for a 1.7m tall object

    [Header("Raycast Settings")]
    public LayerMask floorLayer;
    public LayerMask enemyLayer;
    public float rayDistance = 20f;

    [Header("Visuals")]
    public Material highlightMaterial; // The Yellow transparent material
    public Color ghostColor = new Color(0, 1, 0, 0.5f); // Green transparent for ghost

    // Internal State
    private GameObject _currentDraggedObject;
    private GameObject _hoveredObject;
    private Material _originalMaterial; // Store the Material, not just color, for safety

    private GameObject _placementGhost;
    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.positionCount = 2;
        // --- FIX STARTS HERE ---
        // Force the material to Sprites/Default to prevent VR Stereo Rendering glitches
        if (_lineRenderer.material == null || _lineRenderer.material.name.StartsWith("Default-Line"))
        {
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        // --- FIX ENDS HERE ---
        _lineRenderer.startWidth = 0.01f;
        _lineRenderer.endWidth = 0.005f;

        if (prefabToPlace != null)
        {
            // 1. Create the Ghost
            _placementGhost = Instantiate(prefabToPlace);

            // 2. IMPORTANT: Move Ghost and ALL children to "Ignore Raycast" layer (Layer 2)
            // This makes the ray pass through both your solid collider AND your trigger collider.
            SetLayerRecursively(_placementGhost, 2);

            // 3. Remove the HitTest script from the Ghost only
            // (We don't want the ghost changing colors if a bullet hits it while you are aiming)
            var ghostHitTest = _placementGhost.GetComponent<HitTest>();
            if (ghostHitTest) Destroy(ghostHitTest);

            // 4. Make it look like a ghost (Green/Transparent)
            // We loop through all renderers in case your rectangle has child parts
            foreach (var rend in _placementGhost.GetComponentsInChildren<Renderer>())
            {
                Material ghostMat = new Material(Shader.Find("Sprites/Default"));
                ghostMat.color = ghostColor; // Your green transparent color
                rend.material = ghostMat;
            }

            _placementGhost.SetActive(false);
        }
    }

    // Copy this helper function into your script
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void Update()
    {
        Ray ray = new Ray(controllerAnchor.position, controllerAnchor.forward);
        RaycastHit hit;

        // Visual Ray Default (Red = Hit nothing)
        Vector3 endPoint = ray.origin + (ray.direction * rayDistance);
        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.red;

        // --- 1. HANDLE DRAGGING (Priority) ---
        if (_currentDraggedObject != null)
        {
            if (Physics.Raycast(ray, out hit, rayDistance, floorLayer))
            {
                // Apply Height Offset
                Vector3 targetPos = hit.point + (Vector3.up * verticalOffset);
                _currentDraggedObject.transform.position = targetPos;

                // Ray Visuals
                endPoint = hit.point;
                _lineRenderer.startColor = Color.blue;
                _lineRenderer.endColor = Color.blue;
            }

            // Release
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, controller))
            {
                _currentDraggedObject = null;
            }

            DrawRay(endPoint);
            return; // Stop here, don't hover/highlight while dragging
        }

        // --- 2. RAYCAST LOGIC ---
        bool hitSomething = Physics.Raycast(ray, out hit, rayDistance, floorLayer | enemyLayer);

        if (hitSomething)
        {
            endPoint = hit.point;

            // CHECK: Is it an Enemy?
            if (((1 << hit.collider.gameObject.layer) & enemyLayer) != 0)
            {
                // Ray Color (Yellow = Hovering Enemy)
                _lineRenderer.startColor = Color.yellow;
                _lineRenderer.endColor = Color.yellow;

                // Hide Ghost
                if (_placementGhost) _placementGhost.SetActive(false);

                // Highlight Logic
                HandleEnemyHover(hit.collider.gameObject);

                // Inputs
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
                {
                    _currentDraggedObject = hit.collider.gameObject;
                }

                if (OVRInput.GetDown(OVRInput.Button.Two, controller)) // B Button
                {
                    ClearHover(); // Reset color before destroying!
                    Destroy(hit.collider.gameObject);
                }
            }
            // CHECK: Is it the Floor?
            else if (((1 << hit.collider.gameObject.layer) & floorLayer) != 0)
            {
                // Ray Color (Green = Can Place)
                _lineRenderer.startColor = Color.green;
                _lineRenderer.endColor = Color.green;

                ClearHover(); // Ensure no enemy is highlighted

                // Update Ghost
                if (_placementGhost)
                {
                    _placementGhost.SetActive(true);
                    // Apply Height Offset to Ghost
                    _placementGhost.transform.position = hit.point + (Vector3.up * (verticalOffset + 0.02f));
                    _placementGhost.transform.rotation = Quaternion.identity;
                }

                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
                {
                    PlaceNewObject(hit.point);
                }
            }
        }
        else
        {
            // Hit Nothing (Sky)
            if (_placementGhost) _placementGhost.SetActive(false);
            ClearHover();
        }

        // Draw the laser
        DrawRay(endPoint);
    }

    void DrawRay(Vector3 end)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, controllerAnchor.position);
        _lineRenderer.SetPosition(1, end);
    }

    void PlaceNewObject(Vector3 hitPoint)
    {
        // 1. Calculate Spawn Position (with height offset)
        Vector3 spawnPos = hitPoint + (Vector3.up * verticalOffset);

        // 2. Instantiate
        GameObject newObj = Instantiate(prefabToPlace, spawnPos, Quaternion.identity);

        // 3. Give it a Unique Name (Critical for JSON saving)
        newObj.name = $"Target_{System.DateTime.Now.Ticks % 10000}"; // e.g., Target_4921

        // 4. REGISTER WITH DATABASE
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RegisterEnemy(newObj);
            Debug.Log($"[Instructor] Enemy Placed & Registered: {newObj.name}");
        }
        else
        {
            Debug.LogError("[Instructor] CRITICAL: SessionManager not found! Enemy not saved.");
        }
    }

    void HandleEnemyHover(GameObject enemy)
    {
        // If we are already highlighting this specific enemy, stop.
        if (_hoveredObject == enemy) return;

        // If we were highlighting a DIFFERENT enemy, reset that one first.
        ClearHover();

        _hoveredObject = enemy;
        Renderer rend = _hoveredObject.GetComponent<Renderer>();

        if (rend != null)
        {
            _originalMaterial = rend.material; // Save Original
            if (highlightMaterial != null)
            {
                rend.material = highlightMaterial; // Swap to Highlight
            }
        }
    }

    void ClearHover()
    {
        if (_hoveredObject != null)
        {
            Renderer rend = _hoveredObject.GetComponent<Renderer>();
            if (rend != null && _originalMaterial != null)
            {
                rend.material = _originalMaterial; // Revert to Original
            }
            _hoveredObject = null;
            _originalMaterial = null;
        }
    }

    // Add this anywhere in the class
    void OnDisable()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = false;
        if (_placementGhost != null) _placementGhost.SetActive(false);
        ClearHover();
        _currentDraggedObject = null;
    }
}