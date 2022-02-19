using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public enum EquippedWeapon : byte
{
    nothing,
    lasergun
}


[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Energy))]
[RequireComponent(typeof(RocketMovement))]
[RequireComponent(typeof(PlayerManager))]
public class RocketWeaponManager : NetworkBehaviour
{
    public enum Slot { Primary, Seconday };

    //TODO: This should be a prefab attached to a weaponslot of the weapon manager
    private LaserGun laserGun;
    private Health health;
    private Energy energy;
    private RocketMovement rocket;
    private PlayerManager playerManager;

    // Test new projectile shooting
    public GameObject projectilePrefab;


    [Header("Weapon prefabs")]
    public GameObject lasergunPrefab;

    [SyncVar(hook = nameof(OnChangeWeapon))]
    public EquippedWeapon equippedWeapon;

    private bool primaryActive = false;

    public GameObject weaponMountPoint;
    private Test test;

    private void Start()
    {
        laserGun = GetComponent<LaserGun>();
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();
        rocket = GetComponent<RocketMovement>();
        playerManager = GetComponent<PlayerManager>();
    }    

    public void Disable()
    {
        primaryActive = false;
    }

    private void Update()
    {
        if (isLocalPlayer && primaryActive)
        {
            if (laserGun.CanShoot() && energy.CanConsume(laserGun.energyCost))
            {
                Debug.Log($"Client {netId} shooting!");
                CmdShoot(netId);
                laserGun.Shoot();   //To set shottimer and not spam the server. Maybe refactor this?
            }
            

        }
    }

    /*private void Shoot()
    {        
        if (!laserGun.CanShoot() || !energy.CanConsume(laserGun.energyCost))
            return;

        Debug.Log("Local player shooting");
        laserGun.ShootFX();
        Collider2D hitCollider = laserGun.Shoot();
        if (hitCollider != null)
        {
            Health health = hitCollider.gameObject.GetComponent<Health>();
            if (health != null)
            {
                Debug.Log("We hit something with health");
                health.TakeDamage(laserGun.damage);
            }
        }

        //CmdShoot();

        // Add recoil to the shooter
        //rocket.AddForce(-rocket.transform.up, laserGun.recoil * rocket.thrustForce);
    }*/

    [Command]
    private void CmdShoot(uint shooterNetId)
    {
        Debug.Log($"Server: Client {shooterNetId} is shooting");

        if (laserGun.CanShoot() && energy.CanConsume(laserGun.energyCost)) {
            energy.Consume(laserGun.energyCost);
            laserGun.Shoot();

            GameObject projectile = Instantiate(projectilePrefab, weaponMountPoint.transform.position, weaponMountPoint.transform.rotation);
            projectile.GetComponent<LaserShot>().Init(shooterNetId);
            NetworkServer.Spawn(projectile);

            RpcShoot();
        }
        else
            Debug.Log($"Server: Client {shooterNetId} is not allowed to shoot");
        

        //if (!laserGun.CanShoot() || !energy.CanConsume(laserGun.energyCost))        
        //    return;        

        //energy.Consume(laserGun.energyCost);

        //playerManager.SetPlayerLayer(true);
        //Collider2D hitCollider = laserGun.Shoot();
        //playerManager.SetPlayerLayer(false);


        /*if (hitCollider != null)
        {
            Health health = hitCollider.gameObject.GetComponent<Health>();
            if (health != null) {
                Debug.Log("We hit something with health");
                health.TakeDamage(laserGun.damage);
            }
        }*/
        
    }

    [ClientRpc]
    private void RpcShoot()
    {
        laserGun.ShootFX();
        /*Collider2D hitCollider = laserGun.Shoot();
        if (hitCollider != null)
        {
            Health health = hitCollider.gameObject.GetComponent<Health>();
            if (health != null)
            {
                Debug.Log("We hit something with health");
                CmdTakeDamage(laserGun.damage);    
            }
        }*/
    }

    /*[Command]
    private void CmdTakeDamage(float amount)
    {
        health.TakeDamage(amount);
    }*/

    void OnChangeWeapon(EquippedWeapon oldEquippedWeapon, EquippedWeapon newEquippedWeapon)
    {
        StartCoroutine(ChangeWeapon(newEquippedWeapon));
    }

    // Since Destroy is delayed to the end of the current frame, we use a coroutine
    // to clear out any child objects before instantiating the new one
    IEnumerator ChangeWeapon(EquippedWeapon newEquippedItem)
    {
        while (weaponMountPoint.transform.childCount > 0)
        {
            Destroy(weaponMountPoint.transform.GetChild(0).gameObject);
            test = null;
            yield return null;
        }

        switch (newEquippedItem)
        {
            case EquippedWeapon.lasergun:
                GameObject go = Instantiate(lasergunPrefab, weaponMountPoint.transform);
                test = go.GetComponent<Test>();
                break;            
        }
    }

    [Command]
    void CmdChangeEquippedWeapon(EquippedWeapon selectedItem)
    {
        equippedWeapon = selectedItem;
    }

    #region Input

    public void OnFire1Changed(InputAction.CallbackContext context)
    {
        // For testing
        if (context.started)
        {
            //Debug.Log($"Player({netId}) shooting!");
            //CmdShoot(netId);
        }
            

        if (context.performed && !health.IsDead())
            primaryActive = true;


        if (context.canceled && !health.IsDead())
            primaryActive = false;
    }

    public void OnDebug3Changed(InputAction.CallbackContext context)
    {
        if (context.performed && equippedWeapon != EquippedWeapon.nothing)
        {
            CmdChangeEquippedWeapon(EquippedWeapon.nothing);
        }
    }

    public void OnDebug4Changed(InputAction.CallbackContext context)
    {
        if (context.performed && equippedWeapon != EquippedWeapon.lasergun)
        {
            CmdChangeEquippedWeapon(EquippedWeapon.lasergun);
        }
    }

    public void OnDebug5Changed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (test == null)
            {
                Debug.Log("No weapon equipped");
            } else
            {
                test.Hello();
            }
        }
    }

    #endregion




}
