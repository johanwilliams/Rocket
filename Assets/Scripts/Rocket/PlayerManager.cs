using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RocketMovement))]
[RequireComponent(typeof(RocketWeaponManager))]
public class PlayerManager : NetworkBehaviour
{        

    [SerializeField] private string remoteLayerName = "PlayerRemote";
    
    private RocketMovement rocket;
    private RocketWeaponManager weapons;    

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
