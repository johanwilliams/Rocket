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

    [Header("Ground collision")]
    [SerializeField] private bool groundCollisionEnabled = true;
    [SerializeField] [Range(0f, 200f)] private float magnitudeThreshold = 50f;
    [SerializeField] [Range(0f, 1f)] private float damageModifier = 0.1f;
    [SerializeField] private LayerMask test;

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

    /// <summary>
    /// Triggers on collision and checks if collided with the ground layer.
    /// If ground collisions is enabled we check if the magnitude of the collision is greather than the defined
    /// threshold to give damage. If so we take damage using the magnitude times the damage modifier
    /// </summary>
    /// <param name="collision">Collision details</param>    
    private void OnCollisionEnter2D(Collision2D collision)
    {        
        if (isLocalPlayer && groundCollisionEnabled && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            float magnitude = collision.relativeVelocity.magnitude;
            if (magnitude > magnitudeThreshold)
                CmdTakeGroundDamage(magnitude * damageModifier);
        }
    }

    [Command]
    private void CmdTakeGroundDamage(float damage)
    {
        TakeDamage(damage);
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
