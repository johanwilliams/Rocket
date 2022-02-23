using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody2D))]
public class LaserShot : NetworkBehaviour
{    
    private Rigidbody2D rigidBody;    
    private uint shooter;
    private float destroyAfter = 2f;
    public float speed = 200f;
    public float damage = 40;


    private void Start()
    {
        rigidBody = GetComponent<Rigidbody2D>();
    }

    public override void OnStartServer()
    {
        if (destroyAfter > 0)
            Invoke(nameof(DestroySelf), destroyAfter);
    }

    private void Update()
    {        
        transform.position = transform.position + rigidBody.transform.up * speed * Time.deltaTime;
    }

    public void Init(uint shooterNetId)
    {
        shooter = shooterNetId;
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
            health.TakeDamage(damage);
        }

        //Instantiate impact particle system through RPC call
        //Instantiate(impactParticleSystem, end3, Quaternion.LookRotation(hit.normal));

        DestroySelf();        
    }
}
