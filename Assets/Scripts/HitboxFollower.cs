using UnityEngine;

public class HitboxFollower : MonoBehaviour
{
    public Transform player_root;
    public Vector3 center_offset;
    // Update is called once per frame
    void Update()
    {
        transform.position = player_root.position - center_offset;
        transform.rotation = Quaternion.Euler(0,player_root.eulerAngles.y,0);
        Debug.Log("Player position: " + player_root.position);
    }
}
