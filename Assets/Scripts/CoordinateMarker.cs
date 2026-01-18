using UnityEngine;

public class CoordinateMarker : MonoBehaviour
{
    [Header("Settings")]
    public Transform controllerTransform;
    public GameObject markerPrefab;
    public LayerMask floorLayer;
    public OVRInput.Controller controllerType = OVRInput.Controller.RTouch;

    [Header("Saved Data")]
    public Vector3 savedCoordinate;

    // Internal tracking
    private GameObject _previewMarker;
    private GameObject _confirmedMarker;

    void Update()
    {
        // 1. Trigger HOLD: Show the "Preview" marker
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            ShowPreview();
        }

        // 2. Trigger RELEASE: Lock it in
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            ConfirmLocation();
        }
    }

    void ShowPreview()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer))
        {
            // Create Preview if needed
            if (_previewMarker == null)
            {
                _previewMarker = Instantiate(markerPrefab);

                // --- FIX 1: Remove Collider Logic ---
                // We destroy ANY collider on the marker so the Raycast never hits it.
                // This prevents the "Invisible/Floating" glitch.
                Collider c = _previewMarker.GetComponent<Collider>();
                if (c) Destroy(c);

                // Also check children just in case
                foreach (var childC in _previewMarker.GetComponentsInChildren<Collider>())
                    Destroy(childC);

                // --- FIX 2: Disable Shadows ---
                // Shadows on a floor marker look weird/flickery.
                Renderer r = _previewMarker.GetComponent<Renderer>();
                if (r)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false; // Optional
                }
            }

            _previewMarker.SetActive(true);

            // --- FIX 3: Increase Lift ---
            // 0.03f (3cm) is safer to prevent Z-fighting flickering
            Vector3 offsetPosition = hit.point + (hit.normal * 0.03f);

            _previewMarker.transform.position = offsetPosition;

            // --- FIX 4: Correct Rotation ---
            // If using a Quad, LookRotation(normal) makes it face DOWN (invisible).
            // LookRotation(-normal) makes it face UP (visible).
            _previewMarker.transform.rotation = Quaternion.LookRotation(-hit.normal);
        }
        else
        {
            // Hide if aiming at sky
            if (_previewMarker != null) _previewMarker.SetActive(false);
        }
    }

    void ConfirmLocation()
    {
        // Only confirm if the preview is actually valid/visible
        if (_previewMarker != null && _previewMarker.activeSelf)
        {
            // 1. Delete the OLD confirmed marker
            if (_confirmedMarker != null)
            {
                Destroy(_confirmedMarker);
            }

            // 2. Promote Preview to Confirmed
            _confirmedMarker = _previewMarker;
            _previewMarker = null;

            // 3. Save Coordinate (Remove offset for accurate data)
            savedCoordinate = _confirmedMarker.transform.position - (_confirmedMarker.transform.forward * 0.03f);

            Debug.Log($"Coordinate Saved: {savedCoordinate}");
        }
        else if (_previewMarker != null)
        {
            // We let go of the trigger while looking at the sky -> Cancel
            Destroy(_previewMarker);
            _previewMarker = null;
        }
    }
}