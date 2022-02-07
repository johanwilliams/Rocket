using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Cinemachine;
using UnityEngine.InputSystem;

public class Player2D : NetworkBehaviour
{

    [SyncVar(hook = nameof(OnHolaCountChanged))]
    int holaCount = 0;

    public Rigidbody2D rb;
    public Health health;

    [SerializeField] private float thrustForce = 200.0f;
    [SerializeField] private float rotationSpeed = 150f;

    [SerializeField] private string localLayerName = "PlayerLocal";

    private float rotation;
    private float throttle;

    #region MonoBehaviour api    

    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            GameObject camObj = GameObject.Find("2D Camera");
            CinemachineVirtualCamera vcam = camObj.GetComponent<CinemachineVirtualCamera>();
            vcam.Follow = transform;
            SetLocalPlayerLayer();
        }        
    }

    void SetLocalPlayerLayer()
    {
        int newLayer = LayerMask.NameToLayer(localLayerName);
        gameObject.layer = newLayer;

        foreach (Transform trans in gameObject.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = newLayer;
        }
    }

    void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
    }

    private void Update()
    {        

        /*
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Sending Hola to the server");
            Hola();
        } */       


    }    

    private void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            ThrustForward(throttle * thrustForce);
            Rotate(rotation * -rotationSpeed);
        }
    }

    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            float magnitude = collision.relativeVelocity.magnitude;
            //Debug.Log($"Collided with the Environment with a force of {magnitude}");
            if (magnitude > 20f)
                health.TakeDamage(magnitude * 0.25f);
        }
    }

    #endregion

    #region Input Management

    public void OnThrottleChanged(InputAction.CallbackContext context)
    {
        throttle = context.ReadValue<float>();
    }

    public void OnRotationChanged(InputAction.CallbackContext context)
    {
        rotation = context.ReadValue<Vector2>().x;
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
