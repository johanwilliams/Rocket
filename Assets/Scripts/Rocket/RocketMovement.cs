using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
public class RocketMovement : NetworkBehaviour
{
    [Header("Rocket movement")]
    [SerializeField] private float thrustForce = 200.0f;
    [SerializeField] private float rotationForce = 150f;

    [Header("Rocket boost")]
    [SerializeField] [Range(0f, 3f)] private float boostModifier = 1.5f;
    [SerializeField] [Range(0f, 50f)] private float boostEnergyCost = 30f;

    private Rigidbody2D rb;
    public float rotation { get; set; }
    public float throttle { get; set; }

    public bool boost { get; set; } = false;

    private Health health;
    private Energy energy;
    private AudioSource thrusterSound;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();
        thrusterSound = GetComponent<AudioSource>();
    }

    private void FixedUpdate()
    {
        ThrustForward(throttle * thrustForce);
        Rotate(rotation * -rotationForce);
    }

    private void Update()
    {       
        // For the local player bossting, check if we afford the energy cost and consume it. If not disable boost.
        if (isLocalPlayer && boost && throttle > 0f)
        {
            float energyNeeded = Time.deltaTime * boostEnergyCost;
            if (energy.CanConsume(energyNeeded))
                energy.Consume(energyNeeded);
            else
                boost = false;
        }

        
        float pitch = throttle * (boost ? boostModifier : 1f);
        thrusterSound.pitch = pitch;

        if (throttle > 0 && !thrusterSound.isPlaying)
            thrusterSound.Play();
        if (throttle == 0 && thrusterSound.isPlaying)
            thrusterSound.Pause();
    }

    /// <summary>
    /// Stops all movement
    /// </summary>
    public void Disable()
    {
        throttle = 0f;
        rotation = 0f;
        boost = false;
    }

    #region Rocket movement    

    private void ThrustForward(float amount)
    {
        rb.AddForce(rb.transform.up * amount * (boost ? boostModifier : 1f));
    }

    private void Rotate(float amount)
    {
        rb.AddTorque(amount);
    }

    #endregion

    #region Input

    public void OnThrottleChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
        {
            throttle = context.ReadValue<float>();
        }
            
    }

    public void OnBoostChanged(InputAction.CallbackContext context)
    {
        if (context.performed && !health.IsDead())
            boost = true;

        if (context.canceled && !health.IsDead())
            boost = false;
    }

    public void OnRotationChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
            rotation = context.ReadValue<Vector2>().x;
    }

    #endregion

}
