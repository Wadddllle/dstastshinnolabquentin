using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float life = 3f;
    public float dmg = 50f;
    public GameObject explosionPrefab;
    public GameObject bloodSplatterPrefab;

    [SerializeField] private string envLayerName = "Environment";
    [SerializeField] private string enemyLayerName = "Enemy";
    private int envLayerId;
    private int enemyLayerId;

    void Awake()
    {
        envLayerId = LayerMask.NameToLayer(envLayerName);
        enemyLayerId = LayerMask.NameToLayer(enemyLayerName);
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.CompareTag("Destructable")) //destructable terrain/objects
        {
            ContactPoint contact = collision.GetContact(0);
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

        else if (collision.gameObject.layer == enemyLayerId) //enemies
        {
            ContactPoint contact = collision.GetContact(0);
            Health target = collision.gameObject.transform.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDmg(dmg);
                /*if (collision.gameObject.CompareTag("Head"))
                {
                    target.TakeDmg(dmg*2); //headshot x2 dmg
                    if (GridRecorder.Instance != null)
                        GridRecorder.Instance.LogEvent("HIT", $"Headshot on: {target.gameObject.name}", contact.point);
                }
                else if (collision.gameObject.CompareTag("Body"))
                {
                    target.TakeDmg(dmg); //bodyshot normal dmg
                    if (GridRecorder.Instance != null)
                        GridRecorder.Instance.LogEvent("HIT", $"Bodyshot on: {target.gameObject.name}", contact.point);
                }
                else if (collision.gameObject.CompareTag("Limb"))
                {
                    target.TakeDmg(dmg*0.5f); //limbshot x0.5 dmg
                    if (GridRecorder.Instance != null)
                        GridRecorder.Instance.LogEvent("HIT", $"Limbshot on: {target.gameObject.name}", contact.point);
                }*/
                GameObject bloodSplatter = Instantiate(bloodSplatterPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(gameObject);    
            }
        }

        else if (collision.gameObject.layer == envLayerId) //environment
        {
            ContactPoint contact = collision.GetContact(0);
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

