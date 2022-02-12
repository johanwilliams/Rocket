using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RocketMovement))]
[RequireComponent(typeof(RocketWeaponManager))]
[RequireComponent(typeof(Health))]
public class PlayerManager : NetworkBehaviour
{        

    [SerializeField] private string remoteLayerName = "PlayerRemote";

    [Header("Ground collision")]
    [SerializeField] private bool groundCollisionEnabled = true;
    [SerializeField] [Range(0f, 200f)] private float magnitudeThreshold = 50f;
    [SerializeField] [Range(0f, 1f)] private float damageModifier = 0.1f;

    [Header("Death and respawn")]
    [SerializeField] [Range(1, 5)] private float deathDuration = 3f;
    [SerializeField] [Range(1, 5)] private float respawnDuration = 2f;
    [SerializeField] private ParticleSystem deathEffect;
    [SerializeField] private GameObject[] disableGameObjectsOnDeath;
    [SerializeField] private Behaviour[] disableComponentsOnDeath;

    // Components we need access to
    private RocketMovement engine;
    private RocketWeaponManager weaponMgmt;
    private Health health;

    #region MonoBehaviour api    

    /// <summary>
    /// Set up the references to the components we need
    /// </summary>
    void Start()
    {        
        engine = GetComponent<RocketMovement>();
        weaponMgmt = GetComponent<RocketWeaponManager>();
        health = GetComponent<Health>();        

        // As the server we want to hook up to the Health component and be notified when a rocket dies
        if (isServer)
        {
            health.OnDeath += Die;
        }

        // Activate the camera follow on the local player
        if (isLocalPlayer)
        {
            SetCameraFollow();            
        }
        else
        {
            // Set the remote layer on all non local players
            SetRemotePlayerLayer();
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
    private void SetRemotePlayerLayer()
    {
        gameObject.layer = LayerMask.NameToLayer(remoteLayerName);
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
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isLocalPlayer && groundCollisionEnabled && collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            float magnitude = collision.relativeVelocity.magnitude;
            if (magnitude > magnitudeThreshold)
                CmdTakeDamage(magnitude * damageModifier);
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

        // Turn off all movement and weapon input
        engine.throttle = 0f;   //TODO: Better to have some engine.disable function
        engine.rotation = 0f;
        weaponMgmt.SetShooting(RocketWeaponManager.Slot.Primary, false);  //TODO: Better to have some weaponMgmt.disable function
        weaponMgmt.SetShooting(RocketWeaponManager.Slot.Seconday, false);
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
        RpcRespawnTarget(spawnPosition.position, spawnPosition.rotation);
        
        // Wait and then play the spawn effect
        yield return new WaitForSeconds(respawnDuration);
        RpcRespawnAll();
        health.Reset();
    } 

    /// <summary>
    /// Target RPC call on the player that died. Respawns by moving the transform to the new spawn point (player
    /// is invisible). Also enables the colliders so the rocket won't fall through the ground (but it is still
    /// invisible)
    /// </summary>
    /// <param name="position">Position of the new spwan point</param>
    /// <param name="rotation">Rotation of the new spawn point</param>
    [TargetRpc]
    private void RpcRespawnTarget(Vector2 position, Quaternion rotation)
    {
        Debug.Log($"Client: {transform.name} respawning");

        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        ToggleCollider(true);
    }

    /// <summary>
    /// Client RPC call executed on all clients. Will enable all disabled components again (i.e.
    /// make the rocket visible again) and also reactivate the colliders.
    /// Also plays the spawn effect.
    /// </summary>
    [ClientRpc]
    private void RpcRespawnAll()
    {
        Debug.Log($"Client: {transform.name} enabling components and spawn effect");
        // Unhide the rocket and turn on the collider        
        ToggleVisibility(true);
        ToggleCollider(true);
        deathEffect.Play();
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

    #region Input Management

    public void OnThrottleChanged(InputAction.CallbackContext context)
    {

        if (!health.IsDead())
            engine.throttle = context.ReadValue<float>();
    }

    public void OnRotationChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
            engine.rotation = context.ReadValue<Vector2>().x;
    }

    public void OnFire1Changed(InputAction.CallbackContext context)
    {
        if (context.performed && !health.IsDead())
            weaponMgmt.SetShooting(RocketWeaponManager.Slot.Primary, true);

        if (context.canceled && !health.IsDead())
            weaponMgmt.SetShooting(RocketWeaponManager.Slot.Primary, false);
    }

    public void OnDebug1Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Self destruct");
            CmdTakeDamage(100f);
        }
    }

    public void OnDebug2Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Debug action 2");
        }
    }

    public void OnDebug3Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Debug action 3");
        }
    }

    public void OnDebug4Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Debug action 4");
        }
    }

    public void OnDebug5Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Debug action 5");
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