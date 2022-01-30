using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100;

    private void Update()
    {
     
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Telling the server we shoot");
            Shoot();
        }


    }

    [Command]
    void Shoot()
    {
        Debug.Log("Client is shooting!");

        //TODO: Calculate if we hit something


        //But for now just decrese the health of the client who shot        
        health = Mathf.Max(0f, health - 10f);
        TookDamage();
    }

    [TargetRpc]
    void TookDamage()
    {
        Debug.Log("We were shot!");
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
    }

}
