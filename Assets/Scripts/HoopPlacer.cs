using UnityEngine;

public class HoopPlacer : MonoBehaviour
{
    public GameObject hoopPrefab;
    public LayerMask placementLayer; // We'll raycast against scene mesh

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
        {
            // Raycast from right controller
            var ray = new Ray(
                OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch),
                OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch) * Vector3.forward
            );

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, placementLayer))
            {
                Instantiate(hoopPrefab, hit.point, Quaternion.LookRotation(-hit.normal));
            }
        }
    }
}