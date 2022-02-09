using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RocketWeaponManager : NetworkBehaviour
{
    public enum Slot { Primary, Seconday };

    //TODO: This should be a prefab attached to a weaponslot of the weapon manager
    private LaserGun laserGun;

    private void Start()
    {
        laserGun = GetComponent<LaserGun>();
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

    
}
