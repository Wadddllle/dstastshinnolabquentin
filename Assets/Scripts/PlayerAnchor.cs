using UnityEngine;

public class PlayerAnchor : MonoBehaviour
{
    public Transform head; 

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 pos = head.position;
        pos.y = 0f;
        transform.position = pos;
    }
}
