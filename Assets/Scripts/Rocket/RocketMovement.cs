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
    [SerializeField] private float rotationForce = 150f;
    [SerializeField] public float thrustForce = 200.0f;
    

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
        if (!isServer)
            return;

        ThrustForward(thrusterValue * thrustForce);
        Rotate(rotationValue * -rotationForce);
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
        UpdateThruster(0f, false);        
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
        AddForce(rb.transform.up, amount * (thrusterBoost ? boostModifier : 1f));
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

    /// <summary>
    /// Updates the thruster of the rocket from new user input
    /// Will make sure correct thruster effects and sounds are played.
    /// </summary>
    /// <param name="newThrusterValue">Thruster value</param>
    /// <param name="newThrusterBoost">Thruster boost</param>
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

            // Update the thruster boost
            thrusterBoost = newThrusterBoost;
        }

        // Thruster updated
        if (newThrusterValue != thrusterValue)
        {
            // Thrust increased from 0
            if (newThrusterValue > 0 && thrusterValue == 0)
            {
                thrusterSound.Play();
                thrusterFlame.Play();

                // Are we also boosting?
                if (thrusterBoost)
                    thrusterBoostFlame.Play();
            }
            // Thrust decreased to 0
            else if (newThrusterValue == 0 && thrusterValue > 0)
            {
                thrusterSound.Stop();
                thrusterFlame.Stop();

                // If thrusters stop the boost also needs to be stopped
                if (thrusterBoost) { 
                    thrusterBoost = false;
                    thrusterBoostFlame.Stop();
                }
            }

            // Update the thruster
            thrusterValue = newThrusterValue;            
        }

        // Something has changed (either thruster or boost) so we need to update the pitch of the thruster sound
        thrusterSound.pitch = thrusterValue * (thrusterBoost ? boostModifier : 1f);

        // Tell the server to update all other clients
        if (isLocalPlayer)
            CmdUpdateThruster(thrusterValue, thrusterBoost);                
    }

    /// <summary>
    /// Server command to pass on the updated thruster values to all other clients
    /// </summary>
    /// <param name="thrusterValue">Thruster value</param>
    /// <param name="thrusterBoost">Boost</param>
    [Command]
    private void CmdUpdateThruster(float thrusterValue, bool thrusterBoost)
    {
        RpcUpdateThruster(thrusterValue, thrusterBoost);
    }

    /// <summary>
    /// Client RPC to update all clients on the updated thruster values.
    /// Excludes the owner (localplayer) though as this is already updated for the local player
    /// </summary>
    /// <param name="thrusterValue">Thruster value</param>
    /// <param name="thrusterBoost">Boost</param>
    [ClientRpc(includeOwner = false)]
    private void RpcUpdateThruster(float thrusterValue, bool thrusterBoost)
    {
        UpdateThruster(thrusterValue, thrusterBoost);
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
        if (!health.IsDead())
        {
            UpdateThruster(context.ReadValue<float>(), thrusterBoost);
        }            
    }

    public void OnBoostInputChanged(InputAction.CallbackContext context)
    {
        bool newThrusterBoost = thrusterBoost;
        if (context.performed && !health.IsDead())
            newThrusterBoost = true;

        if (context.canceled && !health.IsDead())
            newThrusterBoost = false;

        UpdateThruster(thrusterValue, newThrusterBoost);

    }

    public void OnRotationInputChanged(InputAction.CallbackContext context)
    {
        if (!health.IsDead())
            UpdateRotation(context.ReadValue<Vector2>().x);            
    }

    #endregion

}
