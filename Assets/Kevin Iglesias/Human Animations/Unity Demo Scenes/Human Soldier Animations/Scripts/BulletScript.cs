using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float life = 3f;
    public float dmg = 20f;
    public GameObject explosionPrefab;
    public GameObject bloodSplatterPrefab;


    void Awake()
    {
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Destructable")) //destructable terrain/objects
        {
            Health target = collision.gameObject.transform.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                if (target.currentHealth <= 0)
                    Destroy(collision.gameObject);
                GameObject explosion = Instantiate(explosionPrefab, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
                Destroy(gameObject);     
            }
        }

        else if (collision.gameObject.CompareTag("Enemy")) //enemies
        {
            Health target = collision.gameObject.transform.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                GameObject bloodSplatter = Instantiate(bloodSplatterPrefab, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
                Destroy(gameObject);
            }
        }

        else if (collision.gameObject.CompareTag("Indestructable Env")) //environment
        {
            Destroy(gameObject);
            GameObject explosion = Instantiate(explosionPrefab, collision.gameObject.transform.position, collision.gameObject.transform.rotation);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            Physics.IgnoreCollision(collision.collider, gameObject.GetComponent<Collider>());
    }

}

