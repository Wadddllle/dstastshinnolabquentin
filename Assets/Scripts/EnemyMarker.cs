using UnityEngine;
using System.Collections.Generic;

public class EnemyMarker : MonoBehaviour
{
    [Header("Settings")]
    public Transform controllerTransform;
    public GameObject markerPrefab;
    public LayerMask floorLayer;
    public OVRInput.Controller controllerType = OVRInput.Controller.RTouch;


    void Update()
    {
        // Trigger RELEASE: Lock it in
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, controllerType))
        {
            ConfirmLocation();
        }

        if (OVRInput.GetDown(OVRInput.Button.Two, controllerType))
        {
            DeleteMarkerLookedAt();
        }
    }

    void ConfirmLocation()
    {
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, floorLayer) && Vector3.Dot(Vector3.up,hit.normal) >= 0.707f)
        {
            Vector3 offsetPosition = hit.point + (hit.normal * 0.05f);
            GameObject marker = Instantiate(markerPrefab, offsetPosition, Quaternion.LookRotation(-hit.normal));
            Renderer r = marker.GetComponent<Renderer>();
            if (r)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false; // Optional
            }
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

                Debug.Log("[Instructor] Enemy Spawn Point deleted.");
            }
        }
    }
}