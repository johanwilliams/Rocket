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

    [SyncVar(hook = nameof(OnChangeWeapon))]
    public EquippedWeapon equippedWeapon;

    private bool primaryActive = false;
    private Weapon primaryWeapon;

    public GameObject weaponMountPoint;

    private void Start()
    {
        health = GetComponent<Health>();
        energy = GetComponent<Energy>();
        rocket = GetComponent<RocketMovement>();
        playerManager = GetComponent<PlayerManager>();

        if (isLocalPlayer)
            CmdChangeEquippedWeapon(EquippedWeapon.lasergun);
    }
    

    public void Disable()
    {
        primaryActive = false;
    }

    private void Update()
    {
        if (isLocalPlayer && primaryActive && primaryWeapon != null)
        {
            ShootPrimary();
            if (primaryWeapon.fireRate == 0)
                primaryActive = false;
        }
    }

    private void ShootPrimary()
    {
        if (primaryWeapon.CanShoot() && energy.CanConsume(primaryWeapon.energyCost))
        {
            Debug.Log($"Client {netId} shooting with {primaryWeapon.displayName}!");
            primaryWeapon.Shoot();

            CmdShoot(netId);
        }
    }

    [Command]
    private void CmdShoot(uint shooterNetId)
    {
        Debug.Log($"Server: Client {shooterNetId} is shooting");

        //TODO: We should check if we can shoot here as well (laserGun.CanShoot(NetworkTime.time) to avoid cheat but not working even using NetworkTime.time
        if (energy.CanConsume(primaryWeapon.energyCost)) {
            energy.Consume(primaryWeapon.energyCost);
            primaryWeapon.Shoot();

            GameObject projectile = Instantiate(primaryWeapon.shotPrefab, primaryWeapon.firePoint.transform.position, weaponMountPoint.transform.rotation);
            projectile.GetComponent<LaserShot>().Init(shooterNetId, primaryWeapon.timeToLive, primaryWeapon.speed);
            NetworkServer.Spawn(projectile);

            RpcShoot();            
        }
        else
            Debug.Log($"Server: Client {shooterNetId} is not allowed to shoot");
    }

    [ClientRpc]
    private void RpcShoot()
    {
        primaryWeapon.ShootFX();
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
        if (context.performed)
        {

        }
    }

    #endregion




}
