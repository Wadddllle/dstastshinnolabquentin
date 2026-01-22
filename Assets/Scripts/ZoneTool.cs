using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class ZoneTool : MonoBehaviour
{
    [Header("Settings")]
    public Transform rightHandAnchor;
    public LayerMask floorLayer;
    public float rayDistance = 10f;
    public Material lineMaterial; // Drag a Red Unlit material here

    // Internal State
    private List<Vector3> _points = new List<Vector3>();
    private LineRenderer _lineRenderer;
    private GameObject _previewCursor; // Small sphere at tip of laser
    private List<GameObject> _cornerMarkers = new List<GameObject>(); // Visual spheres at corners

    private bool _isZoneLocked = false;

    void Start()
    {
        SetupLineRenderer();
        CreateCursor();
    }

    void OnDisable()
    {
        // Hide visuals when tool is swapped out
        if (_previewCursor) _previewCursor.SetActive(false);
        if (_lineRenderer) _lineRenderer.enabled = false;
        // We do NOT destroy the markers, so the instructor can still see the boundary they made
    }

    void OnEnable()
    {
        if (_previewCursor) _previewCursor.SetActive(true);
        if (_lineRenderer) _lineRenderer.enabled = true;
    }

    void Update()
    {
        if (_isZoneLocked) return; // If finished, do nothing

        Ray ray = new Ray(rightHandAnchor.position, rightHandAnchor.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance, floorLayer))
        {
            // 1. Move Cursor
            _previewCursor.SetActive(true);
            _previewCursor.transform.position = hit.point;

            // 2. Update "Preview Line" (Rubber banding effect)
            UpdateLineVisuals(hit.point);

            // 3. INPUT: Add Point (Trigger)
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                AddPoint(hit.point);
            }

            // 4. INPUT: Finish / Close Loop (Grip)
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
            {
                FinishZone();
            }

            // 5. INPUT: Undo (B Button)
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                UndoLastPoint();
            }
        }
        else
        {
            _previewCursor.SetActive(false);
        }
    }

    void AddPoint(Vector3 pt)
    {
        // Lift point slightly (5cm) so line doesn't z-fight with floor
        Vector3 liftedPt = pt + Vector3.up * 0.05f;

        _points.Add(liftedPt);

        // Spawn a visual marker (Small Sphere)
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = liftedPt;
        marker.transform.localScale = Vector3.one * 0.1f; // 10cm sphere
        Destroy(marker.GetComponent<Collider>()); // No physics
        marker.GetComponent<Renderer>().material.color = Color.red;

        _cornerMarkers.Add(marker);

        Debug.Log($"[ZoneTool] Point Added. Total: {_points.Count}");
    }

    void UndoLastPoint()
    {
        if (_points.Count > 0)
        {
            _points.RemoveAt(_points.Count - 1);

            // Remove visual marker
            GameObject lastMarker = _cornerMarkers[_cornerMarkers.Count - 1];
            Destroy(lastMarker);
            _cornerMarkers.RemoveAt(_cornerMarkers.Count - 1);

            Debug.Log("[ZoneTool] Undo Performed.");
        }
    }

    void FinishZone()
    {
        if (_points.Count < 3)
        {
            Debug.LogWarning("[ZoneTool] Need at least 3 points to make a zone!");
            return;
        }

        _isZoneLocked = true;
        _previewCursor.SetActive(false);

        // Close the visual loop
        _lineRenderer.positionCount = _points.Count + 1;
        _lineRenderer.SetPositions(_points.ToArray());
        _lineRenderer.SetPosition(_points.Count, _points[0]); // Connect back to start

        // SEND TO MANAGER
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.SetZonePoints(_points);
            Debug.Log($"[ZoneTool] ZONE SAVED! {_points.Count} points sent to SessionManager.");
        }
    }

    void UpdateLineVisuals(Vector3 currentAimPos)
    {
        // If we have no points, don't draw line
        if (_points.Count == 0)
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        // Draw lines between confirmed points + rubber band to cursor
        _lineRenderer.positionCount = _points.Count + 1;

        for (int i = 0; i < _points.Count; i++)
        {
            _lineRenderer.SetPosition(i, _points[i]);
        }

        // The last point connects to where we are looking (rubber band)
        _lineRenderer.SetPosition(_points.Count, currentAimPos + Vector3.up * 0.05f);
    }

    void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;

        if (lineMaterial != null)
            _lineRenderer.material = lineMaterial;
        else
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        _lineRenderer.startColor = Color.red;
        _lineRenderer.endColor = Color.red;
    }

    void CreateCursor()
    {
        _previewCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(_previewCursor.GetComponent<Collider>());
        _previewCursor.transform.localScale = Vector3.one * 0.05f; // Tiny cursor
        _previewCursor.GetComponent<Renderer>().material.color = Color.yellow;
        _previewCursor.SetActive(false);
    }
}