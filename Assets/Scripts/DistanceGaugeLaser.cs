using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistanceGaugeLaser : MonoBehaviour
{
    [Range(1f, 10f)]
    public float distance = 5f;

    public LayerMask layers;
    public LaserDot laserDot;
    private LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.enabled = AppManager.Instance.IsInstructorState();
        Vector3 start = transform.position;
        Vector3 direction = transform.forward;
        lineRenderer.SetPosition(0,start);

        if (Physics.Raycast(start, direction, out RaycastHit hit, distance, layers))
        {
            lineRenderer.SetPosition(1, hit.point);
            laserDot.ChangeMaterial(true);
        }
        else
        {
            lineRenderer.SetPosition(1, start + direction * distance);
            laserDot.ChangeMaterial(false);
        }
        
            
    }

    public void SetLaserLength(float length)
    {
        distance = length;  
    }
}
