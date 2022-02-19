using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class LaserShot : NetworkBehaviour
{

    public float destroyAfter = 2;
    public Rigidbody2D rigidBody;
    public float force = 1000;
    private uint shooter;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        rigidBody.AddForce(transform.up * force);
    }

    public override void OnStartServer()
    {
        Invoke(nameof(DestroySelf), destroyAfter);
    }

    public void Init(uint shooterNetId)
    {
        Debug.Log($"Setting shooter of lasershot to player {shooterNetId}");
        shooter = shooterNetId;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
    
/*    [ServerCallback]
    void OnTriggerEnter2D(Collider2D co)
    {
        Debug.Log($"COLLIDING with {co.gameObject.name}");
        NetworkServer.Destroy(gameObject);
    }*/

    // ServerCallback because we don't want a warning
    // if OnCollisionEnter2D is called on the client
    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Only check for a hit and deal damage on the server
        if (!isServer)
            return;

        Debug.Log($"Lasershot hitting {collider.gameObject.name}");
        
        // See if we hit something with health
        GameObject go = collider.gameObject;
        Health health = go.GetComponent<Health>();

        if (health != null)
        {
            NetworkIdentity networkIdentity = go.GetComponent<NetworkIdentity>();
            if (networkIdentity != null && networkIdentity.netId == shooter)
            {
                Debug.Log($"COLLIDING with SELF");
                return;
            }
            health.TakeDamage(40f);
        }            

        DestroySelf();        
    }
}
