using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class LaserGun : NetworkBehaviour
{
    public Transform laserTransform;
    public LineRenderer line;

    [SerializeField] private float range = 100.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.Space))
        {
            //We want to shoot
            Shoot();
        }
    }

    [Command]
    public void Shoot()
    {
        //do a raycast to see where we hit
        RaycastHit2D hit = Physics2D.Raycast(laserTransform.position, laserTransform.up, range);
        if (hit.collider != null)
        {
            // We hit something - draw the line
            var health = hit.collider.gameObject.GetComponent<Health>();
            if (health)
            {
                health.TakeDamage(20f);
            }


            DrawLaser(laserTransform.position, hit.point);

        } else
        {
            DrawLaser(laserTransform.position, laserTransform.position + laserTransform.up * range);
        }
    }

    [ClientRpc]
    void DrawLaser(Vector2 start, Vector2 end)
    {
        StartCoroutine(LaserFlash(start, end));
    }

    IEnumerator LaserFlash(Vector2 start, Vector2 end)
    {
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        yield return new WaitForSeconds(0.3f);
        line.SetPosition(0, Vector2.zero);
        line.SetPosition(1, Vector2.zero);
    }
}
