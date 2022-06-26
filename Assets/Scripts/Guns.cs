using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guns : MonoBehaviour
{
    const float RAY_OFFSET = 0.5f;
    
    [SerializeField] Transform gunHolder;
    [SerializeField] Transform cameraHolder;
    
    GunController currentGun;

    internal int currentGunType { get; private set; } = 4;

    bool canFire = true;
    float cooldownElapsed;
    internal bool reloading { get; private set; } = false;

    internal int ammo { get; private set; }
    internal Transform aimTransform;
    internal Transform effectTransform { get => currentGun.effectTransform; }

    Dictionary<int, int> ammoStorage = new Dictionary<int, int>();
        
    bool InitializeGun()
    {
        GunController gunPrefab = Resources.Load<GunController>("Weapons/Weapon" + currentGunType);
        
        if (gunPrefab != null)
        {             
            currentGun = Instantiate(gunPrefab, gunHolder, false);

            currentGun.transform.localPosition = new Vector3(0, -1.32f, 0);

            if (ammoStorage.ContainsKey(currentGunType))
            {
                ammo = ammoStorage[currentGunType];
            }
            else
            {
                ammo = currentGun.maxAmmo;
            }
            return true;
        }
        return false;
    }

    internal void SetGunVisible(bool visible)
    {
        gunHolder.gameObject.SetActive(visible);
    }

    internal Transform aimData { get => aimTransform; }

    internal void PlaceCamera(Transform targetCamera)
    {
        if (targetCamera == null) return;

        if (currentGun == null)
            InitializeGun();

        targetCamera.SetParent(cameraHolder, false);
        targetCamera.localPosition = Vector3.zero;
        targetCamera.localRotation = Quaternion.identity;
        aimTransform = targetCamera;
    }

    internal bool SetGunType(int gunType)
    {
        if (gunType != currentGunType)
        {
            if (ammoStorage.ContainsKey(currentGunType))
            {
                ammoStorage[currentGunType] = ammo;
            }
            else
            {
                ammoStorage.Add(currentGunType, ammo);
            }
            var oldGunType = currentGunType;

            currentGunType = gunType;

            var oldGun = currentGun;

            if (InitializeGun())
            {
                Destroy(oldGun.gameObject);
                reloading = false;
                return true;
            }            
            else
            {
                currentGunType = oldGunType;
                currentGun = oldGun;
            }
        }
        return false;
    }

    internal void ApplyBulletsBonus()
    {
        ammo = currentGun.maxAmmo;
    }

    void Start()
    {
        if (currentGun == null)
            InitializeGun();
    }

    void Update()
    {
        if (!canFire)
        {
            cooldownElapsed -= Time.deltaTime;
            if (cooldownElapsed <= 0)
            {
                canFire = true;
                if (reloading)
                {
                    ammo = currentGun.maxAmmo;
                    currentGun.ReloadFinished();
                    reloading = false;
                }
            }
        }
    }

    void RaycastBullet()
    {        
        var rayDirection = aimData.TransformDirection(Vector3.forward);
        var rayStart = aimData.transform.position + rayDirection * RAY_OFFSET;
        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit))
        {
            var otherGameObject = hit.collider.gameObject;
            var testTarget = otherGameObject.GetComponent<IDamageable>();
            if (testTarget != null)
            {
                testTarget.AddDamage(1);
            }
        }
    }

    internal void Reload()
    {
        if (canFire)
        {
            reloading = true;
            canFire = false;
            cooldownElapsed = currentGun.reloadTime;
            ammo = 0;
            currentGun.Reload();
        }
    }

    internal void ShowShootAnimation()
    {
        currentGun.Fire();
    }

    internal bool Fire(Player player)
    {
        if (canFire)
        {
            if (ammo > 0)
            {
                ammo--;
                canFire = false;
                cooldownElapsed = currentGun.cooldownTime;

                if (currentGunType == 3) //bazuka rocket launch
                {
                    player.LaunchRocket(aimData);
                }
                else if (currentGunType == 4) //fireball
                {
                    player.LaunchRocket(effectTransform);
                    canFire = true;
                    player.SendReloadRPC();
                }
                else
                {
                    RaycastBullet();
                }

                ShowShootAnimation();
                return true;
            }
            else
            {
                Reload();
            }
        }

        return false;
    }
}
