using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float health = 100f;
    [SerializeField] private float respawnTime = 2f;

    [SerializeField] private ParticleSystem deathEffect;

    public void TakeDamage(float damage)
    {
        if (!isServer) return;
        
        health -= damage;
        Debug.Log($"{gameObject.name} taking {damage} damage. Health: {health}");

        if (health <= 0)
        {
            RpcRespawn();
        }
    }    

    [TargetRpc]
    void RpcRespawn()
    {
        Debug.Log("We died! Respawning");
        StartCoroutine(Respawn());
        Reset();
    }

    //TODO: Should not be in the health script but rather some player management script
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);
        this.transform.position = NetworkManager.startPositions[Random.Range(0, NetworkManager.startPositions.Count)].position;
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
        if (health <= 0)
            deathEffect.Play();
    }

    [Command]
    private void Reset()
    {
        health = 100;
    }

}
