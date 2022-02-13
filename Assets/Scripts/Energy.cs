using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Energy : NetworkBehaviour
{    
    [SerializeField]    
    public float maxEnergy { get; private set; } = 100f;
    
    [SyncVar(hook = nameof(OnEnergyChanged))]    
    [SerializeField] 
    private float energy;
    
    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("Energy regenerated per second")] 
    private float regen = 0f;
    
    [SerializeField]
    [Range(0f, 60f)]
    [Tooltip("Time in seconds until regeneration starts from last energy consumed")]
    private float regenDelay = 10f;
    
    // Delegates and Actions called when energy changes
    public delegate void EnergyAction(float oldEnergy, float newEnergy);
    public event EnergyAction OnChange;

    private float regenTimer = 0f;

    #region Monobeahviour

    private void Start()
    {
        Reset();
    }
    
    /// <summary>
    /// Regenerates energy
    /// </summary>
    private void Update()
    {        
        // If energy regeneration is enabled
        if (isServer && regen > 0)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenDelay && energy < maxEnergy)
                energy = Mathf.Clamp(energy + Time.deltaTime * regen, 0, maxEnergy);
        }
    }    

    #endregion

    public void Reset()
    {
        energy = maxEnergy;
    }

    public bool CanConsume(float energyCost)
    {
        return energy >= energyCost;
    }

    /// <summary>
    /// Executed on the server when we want to consume energy
    /// </summary>
    /// <param name="energyCost">Energy we would like to consume</param>
    /// <returns>true if successful (i.e. we had the energy to consume), false if not</returns>
    public bool Consume(float energyCost)
    {
        if (!isServer || energyCost > energy)
            return false;

        regenTimer = 0f;
        energy = Mathf.Clamp(energy - energyCost, 0, maxEnergy);
        Debug.Log($"{gameObject.name} consumed {energyCost} energy and how has {energy} energy left");
        return true;
    }        

    void OnEnergyChanged(float oldEnergy, float newEnergy)
    {
        Debug.Log($"Energy changed from {oldEnergy} to {newEnergy}");
        OnChange(oldEnergy, newEnergy);
    }
}
