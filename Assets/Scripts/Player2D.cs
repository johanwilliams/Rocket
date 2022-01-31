using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player2D : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHolaCountChanged))]
    int holaCount = 0;

    public Rigidbody2D rb;
    public Health health;

    [SerializeField] private float thrustForce = 200.0f;
    [SerializeField] private float rotationSpeed = 150f;    

    private float xAxis;
    private float yAxis;    

    #region MonoBehaviour api

    void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    private void Update()
    {
        // Update input
        xAxis = Input.GetAxis("Horizontal");
        yAxis = Mathf.Max(0f, Input.GetAxis("Vertical"));   // We don't want the rocket to be able to back up        

        if (isLocalPlayer && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Sending Hola to the server");
            Hola();
        }        


    }    

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            ThrustForward(yAxis * thrustForce);
            Rotate(xAxis * -rotationSpeed);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only run on server
        if (!isLocalPlayer)
            return;

        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            float magnitude = collision.relativeVelocity.magnitude;
            //Debug.Log($"Collided with the Environment with a force of {magnitude}");
            if (magnitude > 10f)
                health.TakeDamage(magnitude);
        }
    }

    #endregion

    #region NetworkBehaviour api    

    public override void OnStartServer()
    {
        //rb.isKinematic = false;
    }

    #endregion

    #region Player movement

    private void ThrustForward(float amount)
    {
        rb.AddForce(rb.transform.up * amount);        
    }

    private void Rotate(float amount)
    {    
        rb.AddTorque(amount);
    }

    #endregion

    #region Data syncronization test

    [Command]
    void Hola()
    {
        Debug.Log("Received Hola from client!");
        holaCount++;
        ReplyHola();
    }

    [TargetRpc]
    void ReplyHola()
    {
        Debug.Log("Received Hola from server");
    }

    [ClientRpc]
    void TooHigh()
    {
        Debug.Log("Too high!");
    }

    void OnHolaCountChanged(int oldCount, int newCount)
    {
        Debug.Log($"Hola count changed from {oldCount} to {newCount}");
    }

    #endregion
}
