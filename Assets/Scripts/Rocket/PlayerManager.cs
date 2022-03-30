using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RocketMovement))]
[RequireComponent(typeof(RocketWeaponManager))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
public class PlayerManager : NetworkBehaviour
{        

    [SerializeField] private string layerPlayerLocal = "PlayerLocal";
    [SerializeField] private string layerPlayerRemote = "PlayerRemote";

    [Header("Ground collision")]
    [SerializeField] private bool groundCollisionEnabled = true;
    [SerializeField] [Range(0f, 200f)] private float magnitudeThreshold = 50f;
    [SerializeField] [Range(0f, 1f)] private float damageModifier = 0.1f;

    [Header("Death and respawn")]
    [SerializeField] [Range(1, 5)] private float deathDuration = 3f;
    [SerializeField] [Range(1, 5)] private float respawnDuration = 2f;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private ParticleSystem respawnEffect;
    [SerializeField] private ParticleSystem damageEffect;
    [SerializeField] private GameObject[] disableGameObjectsOnDeath;
    [SerializeField] private Behaviour[] disableComponentsOnDeath;
    [SerializeField] private Healthbar healthbar;

    [Header("Energy")]
    [SerializeField] private Energybar energybar;

    // Components we need access to
    private RocketMovement engine;
    private RocketWeaponManager weaponMgmt;
    private Health health;
    private Energy energy;

    #region MonoBehaviour api    

    /// <summary>
    /// Set up the references to the components we need
    /// </summary>
    void Start()
    {        
        engine = GetComponent<RocketMovement>();
        weaponMgmt = GetComponent<RocketWeaponManager>();
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();

        health.OnDamage += OnHealthChanged;
        energy.OnChange += OnEnergyChanged;

        // As the server we want to hook up to the Health component and be notified when a rocket dies
        if (isServer)
        {
            health.OnDeath += Die;
        }

        // Activate the camera follow on the local player
        if (isLocalPlayer)
        {
            SetCameraFollow();
            
            healthbar = GameObject.FindObjectOfType<Healthbar>();
            healthbar.SetMaxHealth(health.maxHealth);

            energybar = GameObject.FindObjectOfType<Energybar>();
            energybar.SetMaxEnergy(energy.maxEnergy);

            // Set the remote layer on all non local players
            SetPlayerLayer(true);
        }        
    }

    /// <summary>
    /// Sets the target property of the Cinemachiene camera to this tranform
    /// </summary>
    private void SetCameraFollow()
    {
        GameObject camObj = GameObject.Find("2D Camera");
        CinemachineVirtualCamera vcam = camObj.GetComponent<CinemachineVirtualCamera>();
        vcam.Follow = transform;
    }

    /// <summary>
    /// Set the remote layer on non local players. This is used in raycasting to avoid hitting the local player
    /// </summary>
    public void SetPlayerLayer(bool isLocal)
    {        
        if (isLocal)
        {
            Debug.Log("Setting player layer: LOCAL");
            gameObject.layer = LayerMask.NameToLayer(layerPlayerLocal);
        }            
        else
        {
            Debug.Log("Setting player layer: REMOTE");
            gameObject.layer = LayerMask.NameToLayer(layerPlayerRemote);
        }
            
    }

    #endregion

    /// <summary>
    /// Triggers on collision and checks if collided with the ground layer.
    /// If ground collisions is enabled and we are the local player we check if the magnitude of the 
    /// collision is greather than the defined threshold to give damage. If so we take damage using 
    /// the magnitude times the damage modifier
    /// </summary>
    /// <param name="collision">Collision details</param>    
    ///     
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServer)
            return;

        if (groundCollisionEnabled && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            float magnitude = collision.relativeVelocity.magnitude;
            if (magnitude > magnitudeThreshold)
                health.TakeDamage(magnitude * damageModifier);
        }
    }

    void OnHealthChanged(float oldHealth, float newHealth)
    {        
        if (newHealth <= (health.maxHealth / 2f))
        {     
            // When damaged over 50% increase the emission rate of the damage effect
            var ps = damageEffect.emission;
            ps.rateOverTime = Mathf.Max(0f, (health.maxHealth / 2f - newHealth) / (health.maxHealth / 20f));

            if (!damageEffect.isPlaying)
                damageEffect.Play();
        }
        else if (newHealth > (health.maxHealth / 2f))
            damageEffect.Stop();

        if (isLocalPlayer)
        {
            healthbar.SetHealth(newHealth);
        }
            
    }

    void OnEnergyChanged(float oldEnergy, float newEnergy)
    {
        //TODO: This should probably be moved to some UI class
        if (isLocalPlayer)
        {
            energybar.SetEnergy(newEnergy);
        }
    }

    /// <summary>
    /// Called on the server when we die. Will notify all clients of a rocket dying so they can spawn the death particle effect
    /// and disable the rocket (make it invisible and deactivate colliders).
    /// After that the respawn coroutine is called.
    /// </summary>
    [Server]
    private void Die()
    {
        RpcDieAndDisable();
        StartCoroutine(Respawn());        
    }

    /// <summary>
    /// Client RPC call that makes the rocket invisible, disables the collider of the rocket and then plays the
    /// deatch particle effect. We also disable rocket movement and weapon management.
    /// </summary>
    [ClientRpc]
    private void RpcDieAndDisable()
    {
        Debug.Log($"Client: {transform.name} died!");

        // Hide the rocket and turn off the collider
        ToggleVisibility(false);
        ToggleCollider(false);

        // Play death effect
        deathEffect.Play();
        damageEffect.Stop();
        AudioManager.instance.Play("Explosion");    //TODO: This should not be played on the AudioManager but from the Rocket itself to enable 3D sound

        // Disable all movement and weapon input. No moving or shooting if you are dead!
        engine.Stop();
        weaponMgmt.Disable();
    }

    /// <summary>
    /// Coroutine to spawn the dead player in the following steps.
    ///  - Wait for a defined amout of time to allow the deatch effect to be player (while the player is invisible)
    ///  - Get a new spawn position from the Network manager
    ///  - Makes a target RPC call to the player that died so they can move the player to the new spawn point
    ///  - Wait for a defined amout of time to allow before spawning
    ///  - Calls all clients to play the spawn effect
    ///  - Resets the health of the rocket
    /// </summary>
    /// <returns></returns>
    [Server]
    IEnumerator Respawn()
    {
        // Wait for the death effect to play
        Debug.Log($"Server: Waiting {respawnDuration} seconds to respawn {transform.name}");
        yield return new WaitForSeconds(deathDuration);

        // Get a new spawn point and send it to the target player
        Debug.Log($"Server: Respawning and moving {transform.name} and resetting health");
        Transform spawnPosition = NetworkManager.startPositions[Random.Range(0, NetworkManager.startPositions.Count)];
        //RpcRespawnTarget(spawnPosition.position, spawnPosition.rotation);
        gameObject.transform.position = spawnPosition.position;
        gameObject.transform.rotation = spawnPosition.rotation;
        ToggleCollider(true);

        // Play the respawn effect
        RpcRespawnPlayEfx();

        // Wait and then play the spawn effect
        yield return new WaitForSeconds(respawnDuration);
        RpcRespawnEnableRocket();
        health.Reset();
        energy.Reset();
    }

    /// <summary>
    /// Client RPC call executed on all clients. Plays the spawn effect.
    /// </summary>
    [ClientRpc]
    private void RpcRespawnPlayEfx()
    {
        respawnEffect.Play();
    }

    /// <summary>
    /// Client RPC call executed on all clients. Will enable all disabled components again (i.e.
    /// make the rocket visible again) and also reactivate the colliders.
    /// </summary>
    [ClientRpc]
    private void RpcRespawnEnableRocket()
    {
        Debug.Log($"Client: {transform.name} enabling components and spawn effect");
        // Unhide the rocket and turn on the collider        
        ToggleVisibility(true);
        ToggleCollider(true);
    }

    /// <summary>
    /// Enables or disables components on the rocket to make it invisible/visible.
    /// </summary>
    /// <param name="enabled">true to make visible, false to make it invisible</param>
    private void ToggleVisibility(bool enabled)
    {
        // Components
        foreach(Behaviour component in disableComponentsOnDeath)
        {
            component.enabled = enabled ? true : false;
        }

        // Game objects
        foreach (GameObject gameObject in disableGameObjectsOnDeath)
        {
            gameObject.SetActive(enabled);
        }        
    }

    /// <summary>
    /// Enables or disables the rocket coolliders
    /// </summary>
    /// <param name="enabled">true to activate, false to deactivate</param>
    private void ToggleCollider(bool enabled)
    {
        //Colliders
        Collider2D _col = GetComponent<Collider2D>();
        if (_col != null)
        {
            //Debug.Log($"Collider {_col.name}: {enabled}");
            _col.enabled = enabled;
        }


    }

    /// <summary>
    /// Server command to apply damage to a rocket
    /// </summary>
    /// <param name="damage">Damage amount</param>
    [Command]
    private void CmdTakeDamage(float damage)
    {
        health.TakeDamage(damage);
    }

    #region Input

    public void OnDebug1Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Self destruct");
            CmdTakeDamage(health.maxHealth);
        }
    }

    public void OnDebug2Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"Take {health.maxHealth / 10f} damage");
            CmdTakeDamage(health.maxHealth / 10f);
        }
    }    

    #endregion

    #region NetworkBehaviour api    

    /// <summary>
    /// If we have authority we enable the player input on this client
    /// </summary>
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        UnityEngine.InputSystem.PlayerInput playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        playerInput.enabled = true;        
    }

    #endregion

    
}
