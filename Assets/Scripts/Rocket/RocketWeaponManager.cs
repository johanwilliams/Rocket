using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(Health))]
public class RocketWeaponManager : NetworkBehaviour
{
    public enum Slot { Primary, Seconday };

    //TODO: This should be a prefab attached to a weaponslot of the weapon manager
    private LaserGun laserGun;
    private Health health;

    private void Start()
    {
        laserGun = GetComponent<LaserGun>();
        health = GetComponent<Health>();
    }

    public void SetShooting(Slot slot, bool isShooting)
    {
        if (slot == Slot.Primary)
        {
            laserGun.SetShooting(isShooting);
        } else if (slot == Slot.Seconday)
        {

        }
    }

    #region Input

    public void OnFire1Changed(InputAction.CallbackContext context)
    {
        if (context.performed && !health.IsDead())
            SetShooting(RocketWeaponManager.Slot.Primary, true);

        if (context.canceled && !health.IsDead())
            SetShooting(RocketWeaponManager.Slot.Primary, false);
    }

    #endregion


}
