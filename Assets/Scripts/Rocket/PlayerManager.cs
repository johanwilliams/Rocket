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

    private RocketMovement engine;
    private RocketWeaponManager weaponMgmt;
    private Health health;

    #region MonoBehaviour api    

    // Start is called before the first frame update
    void Start()
    {        
        engine = GetComponent<RocketMovement>();
        weaponMgmt = GetComponent<RocketWeaponManager>();
        health = GetComponent<Health>();        

        if (isServer)
        {
            health.OnDeath += Die;
        }

        if (isLocalPlayer)
        {
            SetCameraFollow();            
        }
        else
        {
            SetRemotePlayerLayer();
        }
    }

    private void SetCameraFollow()
    {
        GameObject camObj = GameObject.Find("2D Camera");
        CinemachineVirtualCamera vcam = camObj.GetComponent<CinemachineVirtualCamera>();
        vcam.Follow = transform;
    }

    private void SetRemotePlayerLayer()
    {
        Debug.Log("Setting remote player layer");
        gameObject.layer = LayerMask.NameToLayer(remoteLayerName);
    }    

    #endregion    

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
                CmdTakeDamage(magnitude * damageModifier);
        }
    }

    [Server]
    private void Die()
    {
        RpcDie();
        RpcDisableRocket();
        StartCoroutine(Respawn());
        //RpcSpawn();
    }

    [ClientRpc]
    private void RpcDie()
    {
        Debug.Log($"Client: {transform.name} died! Hiding the rocket and playing explosion");

        // Hide the rocket and turn off the collider
        ToggleVisibility(false);
        ToggleCollider(false);

        deathEffect.Play();
    }

    [ClientRpc]
    private void RpcDisableRocket()
    {
        Debug.Log($"Client: {transform.name}. Turning off all movement and weapon input");
        // Turn off all movement and weapon input
        engine.throttle = 0f;
        engine.rotation = 0f;
        weaponMgmt.SetShooting(RocketWeaponManager.Slot.Primary, false);
        weaponMgmt.SetShooting(RocketWeaponManager.Slot.Seconday, false);
    }

    [Server]
    IEnumerator Respawn()
    {
        Debug.Log($"Server: Waiting {respawnDuration} seconds for respawn");
        yield return new WaitForSeconds(deathDuration);
        Debug.Log($"Server: Respawning and moving transform and resetting health");
        Transform spawnPosition = NetworkManager.startPositions[Random.Range(0, NetworkManager.startPositions.Count)];        
        //transform.position = spawnPosition.position;
        //transform.rotation = spawnPosition.rotation;
        RpcSpawn(spawnPosition.position, spawnPosition.rotation);        
        yield return new WaitForSeconds(respawnDuration);
        RpcSpawnFX();
        health.Reset();
    } 

    [TargetRpc]
    private void RpcSpawn(Vector2 position, Quaternion rotation)
    {
        Debug.Log($"Client: {transform.name} respawning");

        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        ToggleCollider(true);
        // Unhide the rocket and turn on the collider        
        //ToggleComponents(true);
        //deathEffect.Play();        
    }

    [ClientRpc]
    private void RpcSpawnFX()
    {
        Debug.Log($"Client: {transform.name} enabling components and spawn effect");
        // Unhide the rocket and turn on the collider        
        ToggleVisibility(true);
        ToggleCollider(true);
        deathEffect.Play();
    }

    private void ToggleVisibility(bool enabled)
    {
        // Components
        foreach(Behaviour component in disableComponentsOnDeath)
        {
            //Debug.Log($"Component {component.name}: {enabled}");
            component.enabled = enabled ? true : false;
        }

        // Game objects
        foreach (GameObject gameObject in disableGameObjectsOnDeath)
        {
            //Debug.Log($"Game object {gameObject.name}: {enabled}");
            gameObject.SetActive(enabled);
        }        
    }

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

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        UnityEngine.InputSystem.PlayerInput playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        playerInput.enabled = true;        
    }

    #endregion

    
}
