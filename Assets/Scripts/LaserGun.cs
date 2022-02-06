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

    private float timeToFire = 0;

    public Transform firePoint;
    public LineRenderer line;

    
    
    [Client]
    /// <summary>
    /// Run for the local player. 
    /// Check is we can fire (either single fire or burst).
    /// Calls the local Shoot method to do a raycast and render the shot. Then calls the server shoot (to validate) and do the rpc call to other clients.
    /// </summary>
    void Update()
    {
        Debug.DrawLine(firePoint.position, firePoint.position + firePoint.up * range, Color.gray);

        if (!isLocalPlayer)
            return;
        
        if (fireRate == 0)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                Shoot();                
            }
        }
        else
        {
            if (Input.GetButton("Fire1") && Time.time > timeToFire)
            {
                timeToFire = Time.time + 1 / fireRate;
                Shoot();                
            }
        }                
    }

    /// <summary>
    /// Shoot method for the local player. This to avoid the lag of going to the server and back and then render the shot (which will not look good).
    /// Makes a raycast to find a hitpoint and if so renders the shot.
    /// </summary>
    public void Shoot()
    {
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
            RpcDrawLaser(firePoint.position, hit.point);

        } else
        {
            RpcDrawLaser(firePoint.position, firePoint.position + firePoint.up * range);
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
        Debug.Log("Client: Render");
        Debug.DrawLine(start, end, Color.red);
        line.enabled = true;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        yield return new WaitForSeconds(duration);
        line.enabled = false;
        //line.SetPosition(0, Vector2.zero);
        //line.SetPosition(1, Vector2.zero);
    }
}
