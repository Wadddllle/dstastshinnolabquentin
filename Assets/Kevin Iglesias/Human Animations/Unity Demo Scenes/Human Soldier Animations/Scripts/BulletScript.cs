using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float life = 3f;
    public float dmg = 50f;
    public GameObject explosionPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject shooter;

    [SerializeField] private string envLayerName = "Environment";
    [SerializeField] private string enemyLayerName = "Enemy";
    [SerializeField] private string hostageLayerName = "Hostage";
    private int envLayerId;
    private int enemyLayerId;
    private int hostageLayerId;
    private bool hasHit = false;

    void Awake()
    {
        envLayerId = LayerMask.NameToLayer(envLayerName);
        enemyLayerId = LayerMask.NameToLayer(enemyLayerName);
        hostageLayerId = LayerMask.NameToLayer(hostageLayerName);
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) //since OnCollisionEnter can trigger multiple times per frame, this boolean guard is used to prevent a single bullet from hitting multiple hitboxes
            return;
        hasHit = true;
        
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

        else if (collision.gameObject.CompareTag("Player")) //Player kena
        {
            ContactPoint contact = collision.GetContact(0);
            GameObject bloodSplatter = Instantiate(bloodSplatterPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            if (GridRecorder.Instance != null)
            {
                GridRecorder.Instance.LogEvent("INJURY", $"Player got shot", transform.position); //by right location param is supposed to be where enemy got shot, but that is alr recorded in HIT in bullet script
            }
            Destroy(gameObject);

        }

        else if (collision.gameObject.layer == enemyLayerId) //enemies
        {
            ContactPoint contact = collision.GetContact(0);
            Health target = collision.gameObject.transform.GetComponentInParent<Health>();

            if (target != null)
            {
                
                if (collision.gameObject.CompareTag("Head"))
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
                else if (collision.gameObject.CompareTag("Leg"))
                {
                    target.TakeDmg(dmg*0.5f); //limbshot x0.5 dmg
                    if (GridRecorder.Instance != null)
                        GridRecorder.Instance.LogEvent("HIT", $"Legshot on: {target.gameObject.name}", contact.point);
                }
                GameObject bloodSplatter = Instantiate(bloodSplatterPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(gameObject);    
            }
        }

        else if (collision.gameObject.layer == hostageLayerId && shooter.CompareTag("PlayerProjectiles")) //hostage, which can only be shot by the player
        {
            ContactPoint contact = collision.GetContact(0);
            Health target = collision.gameObject.transform.GetComponentInParent<Health>();
            if (target != null)
            {
                target.TakeDmg(target.maxHealth);
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

