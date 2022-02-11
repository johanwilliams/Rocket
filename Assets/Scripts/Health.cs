using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{    
    [SerializeField] private float maxHealth = 100f;
    [SyncVar(hook = nameof(OnHealthChanged))]
    [SerializeField] private float health;   //TODO: Make private when we have UI to show the health
    [SerializeField] private bool destroyOnDeath = false;

    //TODO: Move from Health
    [SerializeField] private float respawnTime = 2f;    

    // Delegate and Action called when health reaches 0 and we die
    public delegate void DiedAction();
    public event DiedAction OnDeath;

    private bool isDead;

    #region Monobeahviour

    private void Start()
    {
        Reset();

        // Subscibe to our own action if we are to destroy this gameobject on death
        if (destroyOnDeath)
            OnDeath += CmdDie;
    }
    

    private void Update()
    {
        if (health <= 0 && !isDead)
        {
            isDead = true;
            if (OnDeath != null)
                OnDeath();
        }
    }

    #endregion

    public void Reset()
    {
        health = maxHealth;
        isDead = false;
    }

    public void TakeDamage(float damage)
    {
        if (!isServer || health <= 0)
            return;

        health = Mathf.Clamp(health - damage, 0, maxHealth);
        Debug.Log($"{gameObject.name} took {damage} damage and now has a health of {health}");

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
    }

    // Called if we die and if we should destoy on death
    [Command]
    private void CmdDie()
    {
        Debug.Log($"{gameObject.name} died!");
        NetworkServer.Destroy(gameObject);
    }

}
