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

    // Delegate and Action called when health reaches 0 and we die
    public delegate void DiedAction();
    public event DiedAction OnDeath;

    // Internal boolean to manage that we "only die once" when health reaches 0
    private bool dead = false;

    #region Monobeahviour

    private void Start()
    {
        Reset();

        // Subscibe to our own action if we are to destroy this gameobject on death
        if (destroyOnDeath)
            OnDeath += CmdDie;
    }
    
    /// <summary>
    /// Checks is health is 0 (and we are not already dead). If so, triggers the OnDeatch action for others to act on
    /// </summary>
    private void Update()
    {
        if (health <= 0 && !dead)
        {
            dead = true;
            if (OnDeath != null)
                OnDeath();
        }
    }

    public bool IsDead()
    {
        return (health <= 0) ? true : false;
    }

    #endregion

    public void Reset()
    {
        health = maxHealth;
        dead = false;
    }

    /// <summary>
    /// Only the server is allowed to deal damage
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(float damage)
    {
        if (!isServer || health <= 0)
            return;

        health = Mathf.Clamp(health - damage, 0, maxHealth);
        Debug.Log($"{gameObject.name} took {damage} damage and now has a health of {health}");
    }        

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
    }

    /// <summary>
    /// Called on the server to destroy this game object if configured to do so
    /// </summary>
    [Command]
    private void CmdDie()
    {
        Debug.Log($"{gameObject.name} died!");
        NetworkServer.Destroy(gameObject);
    }
}
