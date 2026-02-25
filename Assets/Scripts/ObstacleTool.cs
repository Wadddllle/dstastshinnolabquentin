using UnityEngine;
using Oculus.Interaction; // Interaction SDK

[RequireComponent(typeof(LineRenderer))]
public class ObstacleTool : MonoBehaviour
{
    [Header("Controllers")]
    public Transform rightHandAnchor;
    public OVRInput.Controller rightController = OVRInput.Controller.RTouch;
    public OVRInput.Controller leftController = OVRInput.Controller.LTouch;

    [Header("Settings")]
    public GameObject obstaclePrefab;
    public LayerMask floorLayer;
    public LayerMask obstacleLayer;

    [Header("Materials")]
    public Material highlightMaterial;     // Yellow Transparent (Whole Block)
    public Material faceHighlightMaterial; // Cyan Transparent (Specific Face)

    [Header("Tuning")]
    public float rayDistance = 20f;
    public float extrudeSensitivity = 1.0f;

    // --- State ---
    private GameObject _currentObject;
    private GameObject _hoveredObject;
    private Material _originalMaterial;

    // Extrusion State
    private bool _isExtruding = false;
    private Vector3 _extrusionAxis; // Local axis (e.g. 1,0,0)
    private Vector3 _initialHandPos;
    private Vector3 _initialObjScale;
    private Vector3 _initialObjPos;
    private GameObject _faceHighlighter;

    // Dragging State
    private bool _isDragging = false;

    // Visuals
    private LineRenderer _lineRenderer;
    private GameObject _ghost;

    void Start()
    {
        SetupLineRenderer();
        CreateGhost();
        CreateFaceHighlighter();
    }

    void OnDisable()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = false;
        if (_ghost != null) _ghost.SetActive(false);
        if (_faceHighlighter != null) _faceHighlighter.SetActive(false);
        ClearHover();
        _currentObject = null;
        _isDragging = false;
        _isExtruding = false;
    }

    void Update()
    {
        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        RaycastHit hit;
        Vector3 endPoint = ray.origin + (ray.direction * rayDistance);

        // Inputs
        bool rightTriggerDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, rightController);
        bool rightTriggerUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, rightController);
        bool leftGripHold = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, leftController);
        bool bButtonDown = OVRInput.GetDown(OVRInput.Button.Two, rightController);

        // --- STATE 1: EXTRUDING ---
        if (_isExtruding)
        {
            if (!leftGripHold || _currentObject == null)
            {
                EndExtrude();
            }
            else
            {
                PerformExtrude();
                DrawRay(rightHandAnchor.position, _currentObject.transform.position, Color.cyan);
                return;
            }
        }

        // --- STATE 2: DRAGGING ---
        if (_isDragging && _currentObject != null)
        {
            if (Physics.Raycast(ray, out hit, rayDistance, floorLayer))
            {
                // Dynamic Height: Sit on floor
                float halfHeight = _currentObject.transform.localScale.y * 0.5f;
                Vector3 newPos = hit.point;
                newPos.y += halfHeight;
                _currentObject.transform.position = newPos;

                endPoint = hit.point;
                DrawRay(rightHandAnchor.position, endPoint, Color.blue);
            }

            if (rightTriggerUp)
            {
                _isDragging = false;
                _currentObject = null;
            }
            return;
        }

        // --- STATE 3: SEARCHING ---
        if (Physics.Raycast(ray, out hit, rayDistance, floorLayer | obstacleLayer))
        {
            endPoint = hit.point;
            GameObject hitObj = hit.collider.gameObject;

            // A. HIT OBSTACLE
            if (IsInLayerMask(hitObj.layer, obstacleLayer))
            {
                HandleHover(hitObj);

                if (_ghost) _ghost.SetActive(false);

                // Update Highlight Visuals
                UpdateFaceHighlighter(hitObj, hit.normal);

                // Action: EXTRUDE (Left Grip)
                if (leftGripHold)
                {
                    StartExtrude(hitObj, hit.normal);
                }
                // Action: DRAG (Right Trigger)
                else if (rightTriggerDown)
                {
                    _isDragging = true;
                    _currentObject = hitObj;

                    // FIX: Hide the face highlight immediately when dragging starts
                    HideFaceHighlighter();
                }
                // Action: DELETE (B Button)
                else if (bButtonDown)
                {
                    ClearHover();
                    HideFaceHighlighter();
                    Destroy(hitObj);
                }

                DrawRay(rightHandAnchor.position, endPoint, Color.yellow);
            }
            // B. HIT FLOOR
            else if (IsInLayerMask(hitObj.layer, floorLayer))
            {
                ClearHover();
                HideFaceHighlighter();

                // Ghost Logic
                if (_ghost)
                {
                    _ghost.SetActive(true);
                    _ghost.transform.localScale = Vector3.one;
                    _ghost.transform.position = hit.point + (Vector3.up * 0.5f);
                }

                if (rightTriggerDown)
                {
                    PlaceNewObstacle(hit.point);
                }

                DrawRay(rightHandAnchor.position, endPoint, Color.green);
            }
        }
        else
        {
            // Hit Sky
            ClearHover();
            HideFaceHighlighter();
            if (_ghost) _ghost.SetActive(false);
            DrawRay(rightHandAnchor.position, endPoint, Color.red);
        }
    }

    // --- LOGIC: EXTRUSION ---

    void StartExtrude(GameObject target, Vector3 worldNormal)
    {
        // Disable physics/grabbing while editing
        var grabbable = target.GetComponent<Grabbable>();
        if (grabbable) grabbable.enabled = false;

        Vector3 localNormal = target.transform.InverseTransformDirection(worldNormal);
        _extrusionAxis = GetDominantAxis(localNormal);

        _isExtruding = true;
        _currentObject = target;
        _initialHandPos = rightHandAnchor.position;
        _initialObjScale = target.transform.localScale;
        _initialObjPos = target.transform.position;

        // FIX: Do NOT hide highlighter here. We want it to stay and grow.
        // HideFaceHighlighter(); 
    }

    void PerformExtrude()
    {
        Vector3 handDelta = rightHandAnchor.position - _initialHandPos;
        Vector3 localHandDelta = _currentObject.transform.InverseTransformDirection(handDelta);
        float pullDistance = Vector3.Dot(localHandDelta, _extrusionAxis) * extrudeSensitivity;

        // Calculate Scale
        Vector3 absoluteAxis = new Vector3(Mathf.Abs(_extrusionAxis.x), Mathf.Abs(_extrusionAxis.y), Mathf.Abs(_extrusionAxis.z));
        Vector3 changeVector = Vector3.Scale(absoluteAxis, new Vector3(pullDistance, pullDistance, pullDistance));
        Vector3 newScale = _initialObjScale + changeVector;

        // Min Size constraints
        newScale.x = Mathf.Max(0.1f, newScale.x);
        newScale.y = Mathf.Max(0.1f, newScale.y);
        newScale.z = Mathf.Max(0.1f, newScale.z);

        // Calculate Position Shift
        Vector3 scaleDiff = newScale - _initialObjScale;
        Vector3 positionOffsetLocal = Vector3.Scale(scaleDiff, _extrusionAxis) * 0.5f;
        Vector3 positionOffsetWorld = _currentObject.transform.TransformDirection(positionOffsetLocal);

        _currentObject.transform.localScale = newScale;
        _currentObject.transform.position = _initialObjPos + positionOffsetWorld;

        // FIX: Update the highlighter position/size in real-time!
        // We need to convert the local axis back to a World Normal to tell the highlighter where to go
        Vector3 worldNormal = _currentObject.transform.TransformDirection(_extrusionAxis);
        UpdateFaceHighlighter(_currentObject, worldNormal);
    }

    void EndExtrude()
    {
        if (_currentObject != null)
        {
            var grabbable = _currentObject.GetComponent<Grabbable>();
            if (grabbable) grabbable.enabled = true;
        }

        _isExtruding = false;
        _currentObject = null;

        // Optional: Hide it when done, or leave it until you look away
        // HideFaceHighlighter(); 
    }

    // --- VISUALS ---

    void UpdateFaceHighlighter(GameObject target, Vector3 worldNormal)
    {
        if (_faceHighlighter == null) return;
        _faceHighlighter.SetActive(true);

        Vector3 localNormal = target.transform.InverseTransformDirection(worldNormal);
        Vector3 axis = GetDominantAxis(localNormal);

        // 1. Position: Center + (Axis * HalfScale)
        // FIX: Very tiny offset (1mm)
        Vector3 localOffset = Vector3.Scale(axis, target.transform.localScale * 0.5f);
        localOffset += axis * 0.001f;

        _faceHighlighter.transform.position = target.transform.position + target.transform.rotation * localOffset;

        // 2. Rotation: Look INTO the cube (-worldNormal) to force the Front Face OUT
        if (Mathf.Abs(axis.y) > 0.9f)
        {
            _faceHighlighter.transform.rotation = Quaternion.LookRotation(-worldNormal, target.transform.forward);
        }
        else
        {
            _faceHighlighter.transform.rotation = Quaternion.LookRotation(-worldNormal, target.transform.up);
        }

        // 3. Scale: Stretch to match face
        Vector3 s = target.transform.localScale;
        Vector3 quadScale = Vector3.one;

        if (Mathf.Abs(axis.x) > 0.9f) quadScale = new Vector3(s.z, s.y, 1);
        else if (Mathf.Abs(axis.y) > 0.9f) quadScale = new Vector3(s.x, s.z, 1);
        else if (Mathf.Abs(axis.z) > 0.9f) quadScale = new Vector3(s.x, s.y, 1);

        _faceHighlighter.transform.localScale = quadScale;
    }

    //void PlaceNewObstacle(Vector3 point)
    //{
    //    Vector3 spawnPos = point + (Vector3.up * 0.5f);
    //    GameObject obj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
    //    SetupInteractionComponents(obj);
    //}
    void PlaceNewObstacle(Vector3 point)
    {
        // 1. Calculate Spawn Position
        Vector3 spawnPos = point + (Vector3.up * 0.5f);

        // 2. Instantiate
        GameObject obj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);

        // 3. Setup Interaction (Your existing helper function)
        SetupInteractionComponents(obj);

        // 4. Give Unique Name
        obj.name = $"Obstacle_{System.DateTime.Now.Ticks % 10000}";

        // 5. REGISTER WITH DATABASE
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RegisterObstacle(obj);
            Debug.Log($"[Instructor] Obstacle Placed & Registered: {obj.name}");
        }
        else
        {
            Debug.LogError("[Instructor] CRITICAL: SessionManager not found! Obstacle not saved.");
        }
    }

    // --- SETUP & HELPERS ---

    void SetupInteractionComponents(GameObject obj)
    {
        // 1. Rigidbody
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 2. Grabbable & Transformer
        // Note: For best results, set "OneGrabFreeTransformer" in the PREFAB Inspector 
        // and uncheck "Allow Scaling" there.
        var grabbable = obj.GetComponent<Grabbable>();
        if (grabbable == null) grabbable = obj.AddComponent<Grabbable>();

        var transformer = obj.GetComponent<GrabFreeTransformer>();
        if (transformer == null) transformer = obj.AddComponent<GrabFreeTransformer>();

        var interactable = obj.GetComponent<GrabInteractable>();
        if (interactable == null) interactable = obj.AddComponent<GrabInteractable>();

        interactable.InjectRigidbody(rb);
        interactable.InjectOptionalPointableElement(grabbable);
        grabbable.InjectOptionalOneGrabTransformer(transformer);
    }

    private Vector3 GetDominantAxis(Vector3 v)
    {
        float x = Mathf.Abs(v.x); float y = Mathf.Abs(v.y); float z = Mathf.Abs(v.z);
        if (x > y && x > z) return new Vector3(Mathf.Sign(v.x), 0, 0);
        if (y > x && y > z) return new Vector3(0, Mathf.Sign(v.y), 0);
        return new Vector3(0, 0, Mathf.Sign(v.z));
    }
    void HandleHover(GameObject obj)
    {
        if (_hoveredObject == obj) return;
        ClearHover(); _hoveredObject = obj;
        Renderer r = obj.GetComponent<Renderer>();
        if (r) { _originalMaterial = r.material; if (highlightMaterial) r.material = highlightMaterial; }
    }
    void ClearHover()
    {
        if (_hoveredObject && _originalMaterial) _hoveredObject.GetComponent<Renderer>().material = _originalMaterial;
        _hoveredObject = null;
    }
    void HideFaceHighlighter() { if (_faceHighlighter) _faceHighlighter.SetActive(false); }
    void CreateFaceHighlighter()
    {
        _faceHighlighter = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(_faceHighlighter.GetComponent<Collider>());
        Renderer r = _faceHighlighter.GetComponent<Renderer>();
        if (r && faceHighlightMaterial) r.material = faceHighlightMaterial;
        else if (r) r.material.color = new Color(0, 1, 1, 0.5f);
        _faceHighlighter.SetActive(false);
    }
    void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = 0.01f; _lineRenderer.endWidth = 0.01f;
        if (_lineRenderer.material == null) _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }
    void DrawRay(Vector3 start, Vector3 end, Color col)
    {
        _lineRenderer.enabled = true;
        _lineRenderer.SetPosition(0, start); _lineRenderer.SetPosition(1, end);
        _lineRenderer.startColor = col; _lineRenderer.endColor = col;
    }
    void CreateGhost()
    {
        if (!obstaclePrefab) return;
        _ghost = Instantiate(obstaclePrefab);
        if (_ghost.GetComponent<Collider>()) Destroy(_ghost.GetComponent<Collider>());
        if (_ghost.GetComponent<Rigidbody>()) Destroy(_ghost.GetComponent<Rigidbody>());
        Renderer r = _ghost.GetComponent<Renderer>();
        if (r)
        {
            Material gMat = new Material(Shader.Find("Sprites/Default"));
            gMat.color = new Color(0, 1, 0, 0.3f);
            r.material = gMat;
        }
        _ghost.SetActive(false);
    }
    bool IsInLayerMask(int layer, LayerMask mask) => (mask == (mask | (1 << layer)));
}