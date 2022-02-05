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

    
    

    // Update is called once per frame
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

    [Command]
    public void Shoot()
    {
        Debug.Log("SHOOT");

        //do a raycast to see where we hit        
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, firePoint.up, range, hitLayers);        
        if (hit.collider != null)
        {
            Debug.Log($"We hit {hit.collider.name}");
            // We hit something - draw the line
            var health = hit.collider.gameObject.GetComponent<Health>();
            if (health)
            {
                Debug.Log("We hit something with health!");
                health.TakeDamage(damage);
            }


            DrawLaser(firePoint.position, hit.point);

        } else
        {
            DrawLaser(firePoint.position, firePoint.position + firePoint.up * range);
        }
    }

    [ClientRpc]
    void DrawLaser(Vector2 start, Vector2 end)
    {
        StartCoroutine(LaserFlash(start, end));
    }

    IEnumerator LaserFlash(Vector2 start, Vector2 end)
    {
        Debug.DrawLine(start, end, Color.red);
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        yield return new WaitForSeconds(duration);
        line.SetPosition(0, Vector2.zero);
        line.SetPosition(1, Vector2.zero);
    }
}
