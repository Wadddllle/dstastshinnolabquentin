using UnityEngine;

public class EnemyMarkerTool : MonoBehaviour
{
    [Header("Settings")]
    public Transform controllerTransform;
    public GameObject markerPrefab;
    public LayerMask floorLayer;
    public OVRInput.Controller controllerType = OVRInput.Controller.RTouch;
    public MarkerConfig config;

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

        if (OVRInput.GetDown(OVRInput.Button.Two, controllerType))
        {
            DeleteMarkerLookedAt();
        }
    }
    void ShowPreview()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, floorLayer) && Vector3.Angle(Vector3.up, hit.normal) <= 30f)
        {
            // Create Preview if needed
            if (_previewMarker == null)
            {
                
                _previewMarker = Instantiate(markerPrefab);

                Renderer r = _previewMarker.GetComponent<Renderer>();
                if (r)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false; // Optional
                }
            }

            Vector3 offsetPosition = hit.point + (hit.normal * 0.03f);
            _previewMarker.transform.position = offsetPosition;
            _previewMarker.transform.rotation = Quaternion.LookRotation(-hit.normal);

            _previewMarker.SetActive(true);
        }
        else
        {
            // Hide if aiming at sky
            if (_previewMarker != null) _previewMarker.SetActive(false);
        }
    }
    void ConfirmLocation()
    {
        if (_previewMarker != null && _previewMarker.activeSelf)
        {
            _confirmedMarker = _previewMarker;
            _previewMarker = null;
            _confirmedMarker.name = $"SpawnPoint_{System.DateTime.Now.Ticks % 10000}"; // e.g., Target_4921

            if (SessionManager.Instance != null)
                SessionManager.Instance.RegisterSpawnPoint(_confirmedMarker);

            Renderer r = _confirmedMarker.GetComponent<Renderer>();
            if (r)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false; // Optional
            }
            EnemyMarker enemymarker = _confirmedMarker.GetComponent<EnemyMarker>();
            if (enemymarker != null)
                enemymarker.ApplyMarkerConfig(config);
        }
        else if (_previewMarker != null)
        {
            // We let go of the trigger while looking at the sky -> Cancel
            Destroy(_previewMarker);
            _previewMarker = null;
        }
    }
        
    void DeleteMarkerLookedAt()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.CompareTag("EnemySpawnPoint"))
            {
                GameObject marker = hit.collider.gameObject;
                Destroy(marker);

                if (SessionManager.Instance != null)
                    SessionManager.Instance.UnregisterSpawnPoint(marker);

                Debug.Log("[Instructor] Enemy Spawn Point deleted.");
            }
        }
    }
}