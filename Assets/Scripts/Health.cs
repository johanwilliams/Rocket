using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{    
    [SerializeField]    
    public float maxHealth { get; private set; } = 100f;
    
    [SyncVar(hook = nameof(OnHealthChanged))]    
    [SerializeField] 
    private float health;
    
    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("HP regenerated per second")] 
    private float regen = 0f;
    
    [SerializeField]
    [Range(0f, 60f)]
    [Tooltip("Time in seconds until regeneration starts from last taking damage")]
    private float regenDelay = 10f;
    
    [SerializeField] 
    private bool destroyOnDeath = false;

    // Delegates and Actions called when health changes and also when we die
    public delegate void DamageAction(float oldHealth, float newHealth);
    public event DamageAction OnDamage;

    public delegate void DiedAction();
    public event DiedAction OnDeath;

    // Internal boolean to manage that we "only die once" when health reaches 0
    private bool dead = false;

    private float regenTimer = 0f;

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

        // If health regeneration is enabled
        if (isServer && regen > 0)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay && health < maxHealth)
                health = Mathf.Clamp(health + Time.deltaTime * regen, 0, maxHealth);
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

        regenTimer = 0f;
        health = Mathf.Clamp(health - damage, 0, maxHealth);
        Debug.Log($"{gameObject.name} took {damage} damage and now has a health of {health}");
    }        

    void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"Health changed from {oldHealth} to {newHealth}");
        OnDamage(oldHealth, newHealth);
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
