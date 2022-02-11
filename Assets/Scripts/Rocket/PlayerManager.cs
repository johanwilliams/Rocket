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
    [SerializeField] private ParticleSystem deathEffect;

    private RocketMovement rocket;
    private RocketWeaponManager weapons;
    private Health health;

    #region MonoBehaviour api    

    // Start is called before the first frame update
    void Start()
    {        
        rocket = GetComponent<RocketMovement>();
        weapons = GetComponent<RocketWeaponManager>();        

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

    private void Die()
    {
        if (isServer)
            RpcDie();
        else
            CmdDie();
    }

    // Called when a player dies (health <= 0)
    [Command]
    private void CmdDie()
    {
        Debug.Log($"Server: {transform.name} died!");
        RpcDie();
    }

    [ClientRpc]
    private void RpcDie()
    {
        Debug.Log($"Client: {transform.name} died!");

        //TODO: Update score

        deathEffect.Play();

        // StartCoroutine(Respawn());
    }

    [Command]
    private void CmdTakeDamage(float damage)
    {
        health.TakeDamage(damage);
    }

    #region Input Management

    public void OnThrottleChanged(InputAction.CallbackContext context)
    {

        rocket.throttle = context.ReadValue<float>();
    }

    public void OnRotationChanged(InputAction.CallbackContext context)
    {
        rocket.rotation = context.ReadValue<Vector2>().x;
    }

    public void OnFire1Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
            weapons.SetShooting(RocketWeaponManager.Slot.Primary, true);

        if (context.canceled)
            weapons.SetShooting(RocketWeaponManager.Slot.Primary, false);
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

        health = GetComponent<Health>();
        health.OnDeath += Die;
    }

    #endregion

    
}
