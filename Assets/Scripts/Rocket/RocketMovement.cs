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
    [SerializeField] private ParticleSystem thrusterFlame;
    [SerializeField] private ParticleSystem thrusterBoostFlame;

    private Rigidbody2D rb;
    public float rotationValue;

    private float thrusterValue;  
    private bool thrusterBoost;
    
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
        ThrustForward(thrusterValue * thrustForce);
        Rotate(rotationValue * -rotationForce);
    }

    private void Update()
    {       
        // For the local player boosting, check if we afford the energy cost and consume it. If not disable boost.
        if (isLocalPlayer && thrusterBoost && thrusterValue > 0f)
        {
            float energyNeeded = Time.deltaTime * boostEnergyCost;
            if (energy.CanConsume(energyNeeded))
                energy.Consume(energyNeeded);
            else
                UpdateThruster(thrusterValue, false);
        }        
    }

    

    /// <summary>
    /// Stops all movement
    /// </summary>
    public void Disable()
    {
        thrusterValue = 0f;
        rotationValue = 0f;
        thrusterBoost = false;
    }

    #region Rocket movement    

    private void ThrustForward(float amount)
    {
        rb.AddForce(rb.transform.up * amount * (thrusterBoost ? boostModifier : 1f));
    }

    private void Rotate(float amount)
    {
        rb.AddTorque(amount);
    }

    #endregion    

    private void UpdateThruster(float newThrusterValue, bool newThrusterBoost)
    {
        // No need to update
        if (newThrusterValue == thrusterValue && newThrusterBoost == thrusterBoost)
            return;

        // Has the boost updated
        if (newThrusterBoost != thrusterBoost)
        {            
            // Starting to boost (and we have throttle)
            if (newThrusterBoost && newThrusterValue > 0)
                thrusterBoostFlame.Play();

            // Stopping boost
            if (!newThrusterBoost)
                thrusterBoostFlame.Stop();

            thrusterBoost = newThrusterBoost;
        }

        // Thruster updated
        if (newThrusterValue != thrusterValue)
        {
            // Starting throttle
            if (newThrusterValue > 0 && thrusterValue == 0)
            {
                thrusterSound.Play();
                thrusterFlame.Play();

                // Check if we also hold down boost
                if (thrusterBoost)
                    thrusterBoostFlame.Play();
            }
            else if (newThrusterValue == 0 && thrusterValue > 0)
            {
                thrusterSound.Stop();
                thrusterFlame.Stop();

                // Also stop boosting
                thrusterBoost = false;
                thrusterBoostFlame.Stop();
            }

            thrusterValue = newThrusterValue;            
        }

        // Set pitch of the thrustersound
        thrusterSound.pitch = thrusterValue * (thrusterBoost ? boostModifier : 1f);

        // Update all other clients
        if (isLocalPlayer)
            CmdUpdateThruster(thrusterValue, thrusterBoost);                
    }

    [Command]
    private void CmdUpdateThruster(float newThrusterValue, bool newThrusterBoost)
    {
        RpcUpdateThruster(newThrusterValue, newThrusterBoost);
    }

    [ClientRpc(includeOwner = false)]
    private void RpcUpdateThruster(float newThrusterValue, bool newThrusterBoost)
    {
        UpdateThruster(newThrusterValue, newThrusterBoost);
    }

    #region Input

    public void OnThrottleChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
        {
            UpdateThruster(context.ReadValue<float>(), thrusterBoost);
        }            
    }

    public void OnBoostChanged(InputAction.CallbackContext context)
    {
        bool newThrusterBoost = thrusterBoost;
        if (context.performed && !health.IsDead())
            newThrusterBoost = true;

        if (context.canceled && !health.IsDead())
            newThrusterBoost = false;

        UpdateThruster(thrusterValue, newThrusterBoost);

    }

    public void OnRotationChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
            rotationValue = context.ReadValue<Vector2>().x;
    }

    #endregion

}
