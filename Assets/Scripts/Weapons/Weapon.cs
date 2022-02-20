using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public string displayName;

    public float fireRate = 0;
    public float damage = 10f;
    public float recoil = 5f;
    public float energyCost = 5.0f;

    public GameObject shotPrefab;

    public AudioSource weaponSound;
    public ParticleSystem muzzleFlashParticleSystem;

    public Transform firePoint;

    protected double timeToFire = 0;

    protected virtual void Start()
    {
        if (shotPrefab == null)
            Debug.LogError($"No shot prefab defined for weapon {displayName}");

        weaponSound = GetComponent<AudioSource>();
        if (weaponSound == null)
            Debug.LogWarning($"No weaponsound defined for weapon {displayName}");
    }

    public virtual bool CanShoot()
    {
        return Time.time > timeToFire || fireRate == 0;
    }

    public virtual void Shoot()
    {
        // Update shottimer
        timeToFire = Time.time + 1 / fireRate;
    }

    public virtual void ShootFX()
    {
        if (weaponSound != null)
            weaponSound.Play();

        //TODO: Use object pooling
        //muzzleFlashParticleSystem.Play();
    }
}
