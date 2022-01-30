using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100;


    //TODO: THis should be moved from the health class
    [Command]
    void Shoot()
    {
        Debug.Log("Client is shooting!");

        //TODO: Calculate if we hit something


        //But for now just decrese the health of the client who shot        
        health = Mathf.Max(0f, health - 10f);
        TookDamage();
    }
    
    [Command]
    public void TakeDamage(float damage)
    {
        Debug.Log($"Taking {damage} damage");
        health -= damage;

        if (health <= 0)
        {
            Die();
        }
    }

    [TargetRpc]
    void TookDamage()
    {
        Debug.Log("We were shot!");
    }

    [TargetRpc]
    void Die()
    {
        Debug.Log("We died!");
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
    }

}
