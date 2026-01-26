using UnityEngine;

public class BulletCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit an object that has the TargetBehavior script
        // We look in "Parent" because usually the collider is on the hips/spine, but the script is on the root
        TargetBehavior target = collision.gameObject.GetComponentInParent<TargetBehavior>();

        if (target != null)
        {
            // Calculate exact hit point for the log
            Vector3 hitPoint = collision.contacts[0].point;
            target.HitByBullet(hitPoint);
        }

        // Destroy the bullet immediately on impact
        //Destroy(gameObject);
    }
}