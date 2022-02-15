using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
[RequireComponent(typeof(RocketMovement))]
public class RocketWeaponManager : NetworkBehaviour
{
    public enum Slot { Primary, Seconday };

    //TODO: This should be a prefab attached to a weaponslot of the weapon manager
    private LaserGun laserGun;
    private Health health;
    private Energy energy;
    private RocketMovement rocket;

    private bool primaryActive = false;

    private void Start()
    {
        laserGun = GetComponent<LaserGun>();
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();
        rocket = GetComponent<RocketMovement>();
    }    

    public void Disable()
    {
        primaryActive = false;
    }

    private void Update()
    {
        if (isLocalPlayer && primaryActive && laserGun.CanShoot() && energy.CanConsume(laserGun.energyCost))
        {
            //CmdConsumeEnergy(laserGun.energyCost);
            energy.Consume(laserGun.energyCost);
            laserGun.Shoot();
            rocket.AddForce(-rocket.transform.up, laserGun.recoil * rocket.thrustForce);
        }
    }

    #region Input

    public void OnFire1Changed(InputAction.CallbackContext context)
    {
        if (context.performed && !health.IsDead())
            primaryActive = true;

        if (context.canceled && !health.IsDead())
            primaryActive = false;
    }

    #endregion    

    


}
