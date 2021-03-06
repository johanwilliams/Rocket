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

    private Health health;
    private Energy energy;
    private RocketMovement rocket;
    private PlayerManager playerManager;

    [Header("Weapon prefabs")]
    public GameObject lasergunPrefab;
    public GameObject homingMissilePrefab;

    [SyncVar(hook = nameof(OnChangeWeapon))]
    public EquippedWeapon equippedWeapon;

    private bool primaryActive = false;
    private Weapon primaryWeapon;

    public GameObject weaponMountPoint;

    private PrefabPoolManager prefabPoolManager;

    private void Start()
    {
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();
        rocket = GetComponent<RocketMovement>();
        playerManager = GetComponent<PlayerManager>();

        prefabPoolManager = FindObjectOfType<PrefabPoolManager>();

        // Subscribe to death and respawn actions
        health.OnDeath += Die;
        health.OnReset += Reset;

        Reset();
    }
    

    public void Die()
    {
        primaryActive = false;
        if (isLocalPlayer)
            CmdChangeEquippedWeapon(EquippedWeapon.nothing);
    }

    public void Reset()
    {
        if (isLocalPlayer)
            CmdChangeEquippedWeapon(EquippedWeapon.lasergun);
    }

    private void Update()
    {
        if (isLocalPlayer && primaryActive && primaryWeapon != null && primaryWeapon.CanShoot() && energy.CanConsume(primaryWeapon.energyCost))
        {
            this.Log($"Client {netId} shooting with {primaryWeapon.displayName}!");
            primaryWeapon.Shoot();
            
            CmdShoot(netId, primaryWeapon.firePoint.transform.position, weaponMountPoint.transform.rotation);

            if (primaryWeapon.fireRate == 0)
                primaryActive = false;
        }
    }    

    [Command]
    private void CmdShoot(uint shooterNetId, Vector2 _position, Quaternion _rotation)
    {
        this.Log($"Server: Client {shooterNetId} is shooting");

        //TODO: We should check if we can shoot here as well (laserGun.CanShoot(NetworkTime.time) to avoid cheat but not working even using NetworkTime.time
        if (energy.CanConsume(primaryWeapon.energyCost)) {
            energy.Consume(primaryWeapon.energyCost);
            primaryWeapon.Shoot();

            //GameObject projectile = Instantiate(primaryWeapon.shotPrefab, primaryWeapon.firePoint.transform.position, weaponMountPoint.transform.rotation);
            GameObject projectile = Instantiate(primaryWeapon.shotPrefab, _position, _rotation);
            //Object pooling
            //GameObject projectile = Instantiate(primaryWeapon.shotPrefab, primaryWeapon.firePoint.transform.position, weaponMountPoint.transform.rotation);
            projectile.GetComponent<LaserShot>().Init(shooterNetId);
            NetworkServer.Spawn(projectile);

            RpcShoot();            
        }
        else
            this.Log($"Server: Client {shooterNetId} is not allowed to shoot");
    }

    [ClientRpc]
    private void RpcShoot()
    {
        primaryWeapon.ShootFX();
    }

    [Command]
    private void CmdSpawnHomingMissile()
    {
        this.Log("Spawning homing missile");
        Vector2 spawnPosition = weaponMountPoint.transform.position + -weaponMountPoint.transform.up * 50f;        
        GameObject projectile = Instantiate(homingMissilePrefab, spawnPosition, weaponMountPoint.transform.rotation);
        NetworkServer.Spawn(projectile);
    }

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
            primaryWeapon = null;
            yield return null;
        }

        switch (newEquippedItem)
        {
            case EquippedWeapon.lasergun:
                GameObject go = Instantiate(lasergunPrefab, weaponMountPoint.transform);
                primaryWeapon = go.GetComponent<LaserGun>();
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

        // Button pressed
        if (context.started)
        {
            //ShootPrimary();
        }

        // Button down
        if (context.performed && !health.IsDead())
            primaryActive = true;


        // Button released
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
        if (context.started)
        {
            CmdSpawnHomingMissile();
        }
    }

    #endregion




}
