using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class LaserGun : NetworkBehaviour
{
    [SerializeField] private float fireRate = 0;
    [SerializeField] private float damage = 10f;    
    [SerializeField] private float range = 100.0f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private LayerMask hitLayers;

    [SerializeField] private ParticleSystem muzzleFlashParticleSystem;
    [SerializeField] private Transform firePoint;
    [SerializeField] private ParticleSystem impactParticleSystem;
    [SerializeField] private TrailRenderer laserTrail;

    private float timeToFire = 0;

    
    public LineRenderer line;



    #region NetworkBehaviour api    

    public override void OnStartServer()
    {
        //rb.isKinematic = false;
    }

    #endregion


    [Client]
    /// <summary>
    /// Run for the local player. 
    /// Check is we can fire (either single fire or burst).
    /// Calls the local Shoot method to do a raycast and render the shot. Then calls the server shoot (to validate) and do the rpc call to other clients.
    /// </summary>
    void Update()
    {
        Debug.DrawLine(firePoint.position, firePoint.position + firePoint.up * range, Color.gray);

        /*if (!isLocalPlayer)
            return;

        if (fire1 && Time.time > timeToFire)
        {
            timeToFire = Time.time + 1 / fireRate;
            Shoot();
        }*/
    }

    [Client]
    /// <summary>
    /// Shoot method for the local player. This to avoid the lag of going to the server and back and then render the shot (which will not look good).
    /// Makes a raycast to find a hitpoint and if so renders the shot.
    /// </summary>
    public void Shoot()
    {
        if (Time.time > timeToFire) { 
            Debug.Log("Client: Shoot");

            //do a raycast to see where we hit        
            RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.up, range, hitLayers);
            if (hit.collider != null)
            {            
                StartCoroutine(LaserFlash(firePoint.position, hit.point));
            }
            else
            {
                StartCoroutine(LaserFlash(firePoint.position, firePoint.position + firePoint.up * range));
            }
            CmdShoot();
            timeToFire = Time.time + 1 / fireRate;
        }
    }

    /// <summary>
    /// Server shoot method. 
    /// Makes a raycast to find a hitpoint and if so renders the shot. Also checks if what we hit has a <see cref="Health"/> and in that case calls it to take damage.
    /// Makes an RPC call to all clients but the owner to render the shot (as the owner already has rendered it locally)
    /// </summary>
    [Command]
    public void CmdShoot()
    {
        Debug.Log("Server: Shoot");

        Vector2 firePoint2 = new Vector2(firePoint.position.x, firePoint.position.y);

        //do a raycast to see where we hit        
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.up, range, hitLayers);        
        if (hit.collider != null)
        {
            // We hit something - draw the line
            var health = hit.collider.gameObject.GetComponent<Health>();
            if (health)
            {
                Debug.Log("We hit something with health!");
                health.TakeDamage(damage);
            }
            RpcDrawLaser(firePoint2, hit.point);

        } else
        {
            RpcDrawLaser(firePoint2, firePoint.position + firePoint.up * range);
        }
    }

    /// <summary>
    /// Client RPC call exluding the owner.
    /// Starts a coroutine to render the laser shot
    /// </summary>
    /// <param name="start">Start point of the laser shot (which will be the fire point of the gun)</param>
    /// <param name="end">End point of the laser gun (either what we hit or the range of the gun)</param>
    [ClientRpc(includeOwner = false)]
    void RpcDrawLaser(Vector2 start, Vector2 end)
    {
        Debug.Log("RPC: Shoot");
        StartCoroutine(LaserFlash(start, end));
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
