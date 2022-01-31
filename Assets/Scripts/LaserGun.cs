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
            var player = hit.collider.gameObject.GetComponent<Player2D>();
            if (player)
            {
                //that menas it's a player- respawn them
                StartCoroutine(Respawn(hit.collider.gameObject));
            }


            DrawLaser(laserTransform.position, hit.point);

        } else
        {
            DrawLaser(laserTransform.position, laserTransform.position + laserTransform.up * range);
        }
    }

    [Server]    
    IEnumerator Respawn(GameObject go)
    {
        //TODO: This respawn needs refactoring as it is not working as it should and is also located in the wrong script
        
        //Grab connection from player gameobject
        NetworkConnection playerConn = go.GetComponent<NetworkIdentity>().connectionToClient;
        NetworkServer.UnSpawn(go);
        Transform newPos = NetworkManager.singleton.GetStartPosition();
        go.transform.position = newPos.position;
        go.transform.rotation = newPos.rotation;
        yield return new WaitForSeconds(1f);        
        NetworkServer.Spawn(go);
        go.GetComponent<NetworkIdentity>().AssignClientAuthority(playerConn);
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
