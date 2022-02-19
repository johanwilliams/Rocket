using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGun : Weapon
{            

    void Update()
    {
        Debug.DrawLine(firePoint.position, firePoint.position + firePoint.up * 100f, Color.gray);
    }            
    
    /// <summary>
    /// Client RPC call exluding the owner.
    /// Starts a coroutine to render the laser shot
    /// </summary>
    /// <param name="start">Start point of the laser shot (which will be the fire point of the gun)</param>
    /// <param name="end">End point of the laser gun (either what we hit or the range of the gun)</param>
    public override void ShootFX()
    {
        base.ShootFX();

        //TODO: Use object pooling
        //muzzleFlashParticleSystem.Play();
    }
}
