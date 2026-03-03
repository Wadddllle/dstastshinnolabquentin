using UnityEngine;

public class LaserDot : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public Material floating;
    public Material collided;
    public bool isCutShort = false;

    void Update()
    {
       transform.position = lineRenderer.GetPosition(1);
        GetComponent<MeshRenderer>().enabled = AppManager.Instance.IsInstructorState();
    }

    public void ChangeMaterial(bool cutShort)
    {
        //if (isCutShort == cutShort) 
            //return;
        isCutShort = cutShort;

        Renderer renderer = GetComponent<Renderer>();
        if (cutShort)
            renderer.material = collided;
        else
            renderer.material = floating;
        
    }
}
