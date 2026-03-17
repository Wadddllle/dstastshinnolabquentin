using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float currentHealth;
    public float maxHealth = 100f;
    public GameObject DestroyParticle;

    void Start()
    {
        currentHealth = maxHealth;
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
