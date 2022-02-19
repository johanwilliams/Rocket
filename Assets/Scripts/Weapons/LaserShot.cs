using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class LaserShot : NetworkBehaviour
{    
    private Rigidbody2D rigidBody;    
    private uint shooter;
    private float destroyAfter;
    private float force;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
        rigidBody.AddForce(transform.up * force);
    }

    public override void OnStartServer()
    {
        if (destroyAfter > 0)
            Invoke(nameof(DestroySelf), destroyAfter);
    }

    public void Init(uint shooterNetId, float _destroyAfter, float _force)
    {
        destroyAfter = _destroyAfter;
        shooter = shooterNetId;
        force = _force;
    }    

    // destroy for everyone on the server
    [Server]
    void DestroySelf()
    {        
        NetworkServer.Destroy(gameObject);
    }

    
    
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
                Debug.Log($"Colliding with own gameObject - Ignoring...");
                return;
            }
            health.TakeDamage(40f);
        }

        //Instantiate impact particle system through RPC call
        //Instantiate(impactParticleSystem, end3, Quaternion.LookRotation(hit.normal));

        DestroySelf();        
    }
}