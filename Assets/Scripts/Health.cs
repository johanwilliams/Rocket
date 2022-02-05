using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;
    [SerializeField] private float respawnTime = 2f;

    public void TakeDamage(float damage)
    {
        Debug.Log($"Taking {damage} damage");
        health -= damage;

        if (health <= 0)
        {
            Die();
            health = 100;
            StartCoroutine(Respawn(gameObject));
        }
    }

    [Server]
    IEnumerator Respawn(GameObject go)
    {
        //TODO: This respawn needs refactoring as it is not working as it should

        //Grab connection from player gameobject
        NetworkConnection playerConn = go.GetComponent<NetworkIdentity>().connectionToClient;
        NetworkServer.UnSpawn(go);
        Transform newPos = NetworkManager.singleton.GetStartPosition();
        go.transform.position = newPos.position;
        go.transform.rotation = newPos.rotation;
        yield return new WaitForSeconds(respawnTime);
        NetworkServer.Spawn(go);
        go.GetComponent<NetworkIdentity>().AssignClientAuthority(playerConn);
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
