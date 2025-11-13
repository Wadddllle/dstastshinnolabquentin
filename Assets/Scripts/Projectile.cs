using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("The lifetime of the projectile in seconds.")]
    public float lifetime = 5.0f;

    void Start()
    {
        // Schedule the destruction of this GameObject after 'lifetime' seconds.
        Destroy(gameObject, lifetime);
    }
}