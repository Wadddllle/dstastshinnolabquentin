using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float currentHealth;
    public float maxHealth = 100f;
    public GameObject DestroyParticle;

    [SerializeField] private string envLayerName = "Environment";
    [SerializeField] private string enemyLayerName = "Enemy";
    [SerializeField] private string hostageLayerName = "Hostage";
    private int envLayerId;
    private int enemyLayerId;
    private int hostageLayerId;
    void Start()
    {
        currentHealth = maxHealth;
        enemyLayerId = LayerMask.NameToLayer(enemyLayerName);
        hostageLayerId = LayerMask.NameToLayer(hostageLayerName);
    }

    public void TakeDmg(float dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    void Die()
    {
        GameObject destroyEffect = Instantiate(DestroyParticle, transform.position, transform.rotation);
        Destroy(destroyEffect, 1f);
        Destroy(gameObject, 0.8f);
    }
}
