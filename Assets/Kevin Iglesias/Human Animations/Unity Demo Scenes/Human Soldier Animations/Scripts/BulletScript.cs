using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float life = 3f;
    public float dmg = 20f;
    public GameObject explosionPrefab;
    public GameObject bloodSplatterPrefab;

    [SerializeField] private string envLayerName = "Environment";
    private int envLayerId;

    void Awake()
    {
        envLayerId = LayerMask.NameToLayer(envLayerName);
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.CompareTag("Destructable")) //destructable terrain/objects
        {
            ContactPoint contact = collision.contacts[0];
            Health target = collision.gameObject.transform.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                if (target.currentHealth <= 0)
                    Destroy(collision.gameObject);
                GameObject explosion = Instantiate(explosionPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(gameObject);
            }
        }

        else if (collision.gameObject.CompareTag("Enemy")) //enemies
        {
            ContactPoint contact = collision.contacts[0];
            Health target = collision.gameObject.transform.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                GameObject bloodSplatter = Instantiate(bloodSplatterPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(gameObject);
            }
        }

        else if (collision.gameObject.layer == envLayerId) //environment
        {
            ContactPoint contact = collision.contacts[0];
            Destroy(gameObject);
            GameObject explosion = Instantiate(explosionPrefab, contact.point, Quaternion.LookRotation(contact.normal));
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            Physics.IgnoreCollision(collision.collider, gameObject.GetComponent<Collider>());
    }

}

