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
        }        
    }    
    

    /// <summary>
    /// Stops all movement
    /// </summary>
    public void Disable()
    {
        rotationValue = 0f;
        //UpdateThruster(0f, false);        
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
        AddForce(rb.transform.up, amount * (thrusterBoost ? boostMultiplier : 1f));
    }

    public void AddForce(Vector2 direction, float amount)
    {
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

    [Command]
    private void CmdUpdateThrusterBoost(bool newThrusterBoost)
    {
        // Don't enable the boost if there is no throttle
        // TODO: Also check we have enough energy to boost
        if (newThrusterBoost && thrusterValue == 0)
            return;

        thrusterBoost = newThrusterBoost;
    }

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

    void UpdateThrusterPitchSound()
    {
        thrusterSound.pitch = thrusterValue * (thrusterBoost ? boostMultiplier : 1f);
    }


    private void UpdateRotation(float newRotationValue)
    {
        // No need to update
        if (newRotationValue == rotationValue)
            return;

        rotationValue = newRotationValue;

        // Tell the server to update all other clients
        if (isLocalPlayer)
            CmdUpdateRotation(rotationValue);
    }

    [Command]
    private void CmdUpdateRotation(float rotationValue)
    {
        RpcUpdateRotation(rotationValue);
    }

    [ClientRpc(includeOwner = false)]
    private void RpcUpdateRotation(float rotationValue)
    {
        UpdateRotation(rotationValue);
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
            UpdateRotation(context.ReadValue<Vector2>().x);            
    }

    #endregion

}
