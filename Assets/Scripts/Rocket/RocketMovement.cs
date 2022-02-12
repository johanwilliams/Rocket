using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RocketMovement : MonoBehaviour
{   
    [SerializeField] private float thrustForce = 200.0f;
    [SerializeField] private float rotationForce = 150f;

    private Rigidbody2D rb;
    public float rotation { get; set; }
    public float throttle { get; set; }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        ThrustForward(throttle * thrustForce);
        Rotate(rotation * -rotationForce);
    }

    /// <summary>
    /// Stops all movement
    /// </summary>
    public void Stop()
    {
        throttle = 0f;
        rotation = 0f;
    }

    #region Rocket movement

    private void ThrustForward(float amount)
    {
        rb.AddForce(rb.transform.up * amount);
    }

    private void Rotate(float amount)
    {
        rb.AddTorque(amount);
    }

    #endregion
}
