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
    [SerializeField] private float rotationForceMultiplayer = 150f;
    [SerializeField] public float thrusterForceMultiplier = 200.0f;
    

    [Header("Rocket boost")]
    [SerializeField] [Range(0f, 3f)] private float boostMultiplier = 1.5f;
    [SerializeField] [Range(0f, 50f)] private float boostEnergyCost = 30f;
    [SerializeField] private ParticleSystem thrusterFlame;
    [SerializeField] private ParticleSystem thrusterBoostFlame;

    private Rigidbody2D rb;

    private float rotationValue;
    [SyncVar(hook = nameof(OnThrusterChanged))] private float thrusterValue;
    [SyncVar(hook = nameof(OnThrusterBoostChanged))] private bool thrusterBoost;
    
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
        //TODO: Should we only do this on the server? Or would this act as client side prediction if also done on the clients?
        if (!isServer)
            return;

        ThrustForward(thrusterValue * thrusterForceMultiplier);
        Rotate(rotationValue * -rotationForceMultiplayer);
    }

    private void Update()
    {       
        // Check if we afford the energy cost and consume it. If not disable boost.
        if (isServer && thrusterBoost && thrusterValue > 0f)
        {
            float energyNeeded = Time.deltaTime * boostEnergyCost;
            if (energy.CanConsume(energyNeeded))
                energy.Consume(energyNeeded);
            else
                thrusterBoost = false;  // Stop boosting if we don't have enough energy
        }        
    }        

    /// <summary>
    /// Stops all movement
    /// </summary>
    public void Disable()
    {
        rotationValue = 0f;
        thrusterValue = 0f;
        thrusterBoost = false;
    }

    #region Rocket rigid body updates

    /// <summary>
    /// Moves the rigid body of the rocket forward by applying force in the forward direction 
    /// Also checks if the rocket is boosting and in that case increases the forward force with
    /// the rocket boost multiplier.
    /// </summary>
    /// <param name="amount">Amount of forward force to apply</param>
    private void ThrustForward(float amount)
    {
        if (amount > 0)
            AddForce(rb.transform.up, amount * (thrusterBoost ? boostMultiplier : 1f));
    }

    /// <summary>
    /// Applies a force to the rocket
    /// </summary>
    /// <param name="direction">Direction in which the force is added</param>
    /// <param name="amount">Amount of force to apply</param>
    public void AddForce(Vector2 direction, float amount)
    {
        if (amount > 0)
            rb.AddForce(direction * amount);
    }

    /// <summary>
    /// Rotates the rocket rigid body by applying torque
    /// </summary>
    /// <param name="amount">Amount of torque to apply</param>
    private void Rotate(float amount)
    {
        rb.AddTorque(amount);
    }

    #endregion

    #region Thruster updates    

    /// <summary>
    /// Server command that gets called from the local player when the input thuster value has changed.
    /// Updates the thruster value which is a syncvar that gets synced to all clients
    /// </summary>
    /// <param name="newThrusterValue">New thruster input value</param>
    [Command]
    private void CmdUpdateThruster(float newThrusterValue)
    {
        // Make sure client is not sending an invalid value
        newThrusterValue = Mathf.Clamp(newThrusterValue, 0f, 1f);

        // If thrusters stop the boost also needs to be stopped
        if (newThrusterValue == 0 && thrusterBoost)
            thrusterBoost = false;

        thrusterValue = newThrusterValue;

    }

    /// <summary>
    /// Thuster sync var hook that gets called whenever the thurster value of a rocket changes.
    /// Updates the thuster effects (flames etc) and sound
    /// </summary>
    /// <param name="oldThrusterValue">Old thruster value</param>
    /// <param name="newThrusterValue">New thruster value</param>
    void OnThrusterChanged(float oldThrusterValue, float newThrusterValue)
    {
        // Thrust increased from 0
        if (newThrusterValue > 0 && oldThrusterValue == 0)
        {
            thrusterSound.Play();
            thrusterFlame.Play();

            // Are we also boosting?
            if (thrusterBoost && !thrusterBoostFlame.isPlaying)
                thrusterBoostFlame.Play();
        }
        // Thrust decreased to 0
        else if (newThrusterValue == 0 && oldThrusterValue > 0)
        {
            thrusterSound.Stop();
            thrusterFlame.Stop();
        }

        UpdateThrusterPitchSound();
    }

    /// <summary>
    /// Server command that gets called from the local player when the input thuster boost value has changed.
    /// Updates the thruster boost value which is a syncvar that gets synced to all clients
    /// </summary>
    /// <param name="newThrusterBoost">New thruster boost input value</param>
    [Command]
    private void CmdUpdateThrusterBoost(bool newThrusterBoost)
    {
        // Don't enable the boost if there is no throttle
        // TODO: Also check we have enough energy to boost
        if (newThrusterBoost && thrusterValue == 0)
            return;

        thrusterBoost = newThrusterBoost;
    }

    /// <summary>
    /// Thuster boost sync var hook that gets called whenever the thurster boost value of a rocket changes.
    /// Updates the thuster boost effects (flames etc) and sound
    /// </summary>
    /// <param name="oldThrusterBoost">Old thruster boost value</param>
    /// <param name="newThrusterBoost">New thruster boost value</param>
    void OnThrusterBoostChanged(bool oldThrusterBoost, bool newThrusterBoost)
    {
        // Starting to boost
        if (newThrusterBoost)
            thrusterBoostFlame.Play();

        // Stopping boost
        if (!newThrusterBoost)
            thrusterBoostFlame.Stop();

        UpdateThrusterPitchSound();
    }

    /// <summary>
    /// Updates the pitch of the thuster sound based on the current thuster and boost values
    /// </summary>
    void UpdateThrusterPitchSound()
    {
        thrusterSound.pitch = thrusterValue * (thrusterBoost ? boostMultiplier : 1f);
    }    

    /// <summary>
    /// Server command to update the rotation value of the rocket
    /// </summary>
    /// <param name="newRotationValue">New rotation value</param>
    [Command]
    private void CmdUpdateRotation(float newRotationValue)
    {
        // Make sure client is not sending an invalid value
        newRotationValue = Mathf.Clamp(newRotationValue, -1f, 1f);

        rotationValue = newRotationValue;
    }

    #endregion

    #region Input
    
    public void OnThrusterInputChanged(InputAction.CallbackContext context)
    {
        // Don't allow movement input while we are dead
        if (health.IsDead())
            return;
        
        //UpdateThruster(context.ReadValue<float>(), thrusterBoost);
        CmdUpdateThruster(context.ReadValue<float>());
                    
    }

    public void OnBoostInputChanged(InputAction.CallbackContext context)
    {
        // Don't allow movement input while we are dead
        if (health.IsDead())
            return;

        bool newThrusterBoost = thrusterBoost;
        if (context.performed)
            newThrusterBoost = true;

        if (context.canceled)
            newThrusterBoost = false;

        CmdUpdateThrusterBoost(newThrusterBoost);

    }

    public void OnRotationInputChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
            CmdUpdateRotation(context.ReadValue<Vector2>().x);            
    }

    #endregion

}
