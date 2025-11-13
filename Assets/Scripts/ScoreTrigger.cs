using UnityEngine;

public class ScoreTrigger : MonoBehaviour
{
    public HoopManager hoopManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && rb.linearVelocity.y < -0.5f)
            {
                hoopManager.RegisterScore();
            }

            Debug.Log("Ball entered trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            hoopManager.ResetCanScore();
        }
    }
}
