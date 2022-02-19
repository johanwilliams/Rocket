using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGun : MonoBehaviour
{
    [SerializeField] private float fireRate = 0;
    [SerializeField] public float damage { get; private set; } = 10f;    
    [SerializeField] private float range = 100.0f;
    [SerializeField] [Range(0f, 10f)] public float recoil = 5f;
    [SerializeField] public float energyCost { get; private set; } = 5.0f;

    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private ParticleSystem muzzleFlashParticleSystem;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer laserTrail;

    private float timeToFire = 0;

    /// <summary>
    /// Run for the local player. 
    /// Check is we can fire (either single fire or burst).
    /// Calls the local Shoot method to do a raycast and render the shot. Then calls the server shoot (to validate) and do the rpc call to other clients.
    /// </summary>
    void Update()
    {
        Debug.DrawLine(firePoint.position, firePoint.position + firePoint.up * range, Color.gray);
    }

    public bool CanShoot()
    {
        return Time.time > timeToFire || fireRate == 0;
    }

    /// <summary>
    /// Shoot method for the local player. This to avoid the lag of going to the server and back and then render the shot (which will not look good).
    /// Makes a raycast to find a hitpoint and if so renders the shot.
    /// </summary>
    public void Shoot()
    {           
        //do a raycast to see where we hit        
        //RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.up, range, hitLayers);
                        
        // Update shottimer
        timeToFire = Time.time + 1 / fireRate;

        //return hit.collider;
    }    
    
    /// <summary>
    /// Client RPC call exluding the owner.
    /// Starts a coroutine to render the laser shot
    /// </summary>
    /// <param name="start">Start point of the laser shot (which will be the fire point of the gun)</param>
    /// <param name="end">End point of the laser gun (either what we hit or the range of the gun)</param>
    public void ShootFX()
    {
        // This should not be played from the Audiomanager but the lasergun to give 3D sound
        AudioManager.instance.Play("Laser");
        /*Debug.Log("ShootFX");
        Vector2 start = firePoint.position;
        Vector2 end = firePoint.position + firePoint.up * range;

        //do a raycast to see where we hit        
        RaycastHit2D hit = Physics2D.Raycast(start, firePoint.up, range, hitLayers);
        if (hit.collider != null)
            end = hit.point;


        StartCoroutine(LaserFlash(start, end));*/
    }

    /// <summary>
    /// Coroutine to render the laser shot.
    /// Draws a line and waits a bit before zeroing it out
    /// </summary>
    /// <param name="start">Start point of the laser shot (which will be the fire point of the gun)</param>
    /// <param name="end">End point of the laser gun (either what we hit or the range of the gun)</param>
    /// <returns></returns>
    IEnumerator LaserFlash(Vector2 start, Vector2 end)
    {
        AudioManager.instance.Play("Laser");
        //TODO: Use object pooling
        //muzzleFlashParticleSystem.Play();
        Vector3 start3 = new Vector3(start.x, start.y, 0f);
        Vector3 end3 = new Vector3(end.x, end.y, 0f);

        float time = 0;
        TrailRenderer trail = Instantiate(laserTrail, firePoint.position, Quaternion.identity);

        while (time < 1)
        {
            trail.transform.position = Vector3.Lerp(start3, end3, time);
            time += Time.deltaTime / trail.time;

            yield return null;
        }
        trail.transform.position = end3;

        //Instantiate impact particle system
        //Instantiate(impactParticleSystem, end3, Quaternion.LookRotation(hit.normal));

        Destroy(trail.gameObject, trail.time);        
    }
}
