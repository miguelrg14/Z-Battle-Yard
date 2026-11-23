using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ZBattleYard.Core;

public class WeaponShooting : MonoBehaviour
{
    #region - Variables -
    [SerializeField] SFXManager sfxManager;
    WeaponDecals weaponDecals;
    Camera cam;
    Inventory inventory;
    EquipmentManager equipmentManager;
    [SerializeField] Animator anim;
    PlayerHUD hud;
    GameControls playerInput;
    CameraRecoil cameraRecoil;  /// Recoil
    PlayerStats stats;
    UIManager uiManager;
    PerkManager perkManager;
    StatsManager statsManager;
    Melee melee;

    [SerializeField] LayerMask shootingAvoidLayers; // Layermask to avoid hitting player when shooting

    float lastShotTime = 0;

    public bool canShoot = true;
    public bool canReload = true;

    [Header("Guns")]
    [Header("Primary")]
    [SerializeField] int primaryCurrentAmmo;
    [SerializeField] int primaryCurrentAmmoStorage;
    [SerializeField] public bool primaryMagazineIsEmpty = false;

    [Header("Secondary")]
    [SerializeField] int secondaryCurrentAmmo;
    [SerializeField] int secondaryCurrentAmmoStorage;
    [SerializeField] public bool secondaryMagazineIsEmpty = false;

    [Header("Tertiary")]
    [SerializeField] int tertiaryCurrentAmmo;
    [SerializeField] int tertiaryCurrentAmmoStorage;
    [SerializeField] public bool tertiaryMagazineIsEmpty = false;

    [Header("Camera Shake")]
    [SerializeField] ShakeTransformEventData dataPosition;
    [SerializeField] ShakeTransformEventData dataRotation;
    ShakeTransform shakeHolderTransform;

    [Header("Bullet Impact")]
    [SerializeField] float impactForce = 30f;

    bool isShooting;

    [SerializeField] AudioClip cantShoot_Clip;
    bool canPlay_EmptyMagSound;

    #endregion

    #region - Awake | Start | Update -

    void Awake()    =>  GetReferences();        
    void Start()    =>  Initialize();
    void Update()
    {
        if (isShooting) Shoot();       
    }

    #endregion

    #region - Logic -

    void StartShooting()    =>  isShooting = true;
    void StopShooting()     =>  isShooting = false;

    void Shoot() // Checking if can shot
    {
        if (!stats.IsDead() && !uiManager.GameIsPaused())
        {
            CheckCanShoot(equipmentManager.currentlyEquippedWeapon);

            if (canShoot && canReload) // "canReload" will be false while reloading...
            {
                Weapon currentWeapon = inventory.GetItem(equipmentManager.currentlyEquippedWeapon);
                float fireRate = currentWeapon.fireRate;

                if (PerkManager.bulletLegionActive)     fireRate *= perkManager.GetShotLegionMultiplier();  // x2 damage & Shot speed perk
                if (Time.time > lastShotTime + fireRate)
                {
                    lastShotTime = Time.time;
                    RaycastShot(currentWeapon);    // Actual shot logic (damage, decals...)
                    UseAmmo(equipmentManager.GetCurrentlyEquippedWeapon(), 1, 0); // Use ammo
                }
            }
            else
            {
                if (canPlay_EmptyMagSound)
                    StartCoroutine(EmptyBulletsSound());
            }
        }
    }
    IEnumerator EmptyBulletsSound()
    {
        canPlay_EmptyMagSound = false;
        sfxManager.PlaySound(cantShoot_Clip);
        yield return new WaitForSeconds(1);
        canPlay_EmptyMagSound = true;
    }
    void RaycastShot(Weapon currentWeapon) // Shot
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;
        GunSFXManager.PlayGunSound(currentWeapon.shotSound);
        cameraRecoil.RecoilFire();
        WeaponSway.activateWeaponKnockback = true;

        if (Physics.Raycast(ray, out hit, currentWeapon.range, shootingAvoidLayers))
        {
            ShowVisualFeedback(hit);
            DealDamage(hit, currentWeapon);
            ApplyRigidbodyForces(hit);
            statsManager.UpdateTabbingStats();
        }
        if (inventory.GetItem(equipmentManager.currentlyEquippedWeapon).weaponType != WeaponType.Melee)     Instantiate(currentWeapon.muzzleFlashParticles, equipmentManager.currentWeaponMuzzleFlash);
    }

    void ShowVisualFeedback(RaycastHit hit)
    {
        weaponDecals.Decals(hit);
        shakeHolderTransform.AddShakeEvent(dataPosition);
        shakeHolderTransform.AddShakeEvent(dataRotation);
    }
    void ApplyRigidbodyForces(RaycastHit hit)
    {
        if (hit.rigidbody != null)      hit.rigidbody.AddForce(-hit.normal * impactForce);  // Impact Force knockback
    }

    void DealDamage(RaycastHit hit, Weapon currentWeapon)
    {
        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyHead"))
        {
            CharacterStats enemyStats = hit.transform.GetComponentInParent<CharacterStats>();
            if (enemyStats.IsDead())
                return;
            
            PlayerHUD.instance.UpdateScoreHeadHitAmount();

            if (PowerUpManager.instaKillActive)         // Insta-Kill power-up
            {
                enemyStats.InstantKill();
                return;
            }

            int damage = currentWeapon.damage * 2;      // Headshots deal double damage
            if (PerkManager.headBlowerActive)           damage *= 2;
            if (PerkManager.bulletLegionActive)         damage *= 2;    // Bullet Legion perk: x2 damage and fire rate
            enemyStats.TakeDamage(damage);
            return;
        }
        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyBody"))
        {
            CharacterStats enemyStats = hit.transform.GetComponentInParent<CharacterStats>();
            if (enemyStats.IsDead())
                return;

            PlayerHUD.instance.UpdateScoreHitAmount();

            if (PowerUpManager.instaKillActive)         // Insta-Kill power-up
            {
                enemyStats.InstantKill();
                return;
            }

            int damage = currentWeapon.damage;
            if (PerkManager.bulletLegionActive)         damage *= 2;    // Bullet Legion perk: x2 damage and fire rate
            enemyStats.TakeDamage(damage);
        }
    }

    public void CheckCanShoot(int slot)
    {
        if (slot == 0) // Primary
        {
            if (primaryCurrentAmmo <= 0)    primaryMagazineIsEmpty = true;
            if (primaryCurrentAmmo > 0)     primaryMagazineIsEmpty = false;

            if (primaryMagazineIsEmpty)
            {
                canShoot = false;
                canReload = true;
            }
            else    canShoot = true;
        }
        if (slot == 1) // Secondary
        {
            if (secondaryCurrentAmmo <= 0)  secondaryMagazineIsEmpty = true;
            if (secondaryCurrentAmmo > 0)   secondaryMagazineIsEmpty = false;

            if (secondaryMagazineIsEmpty)
            {
                canShoot = false;
                canReload = true;
            }
            else    canShoot = true;
        }
        if (slot == 2) // Tertiary
        {
            if (tertiaryCurrentAmmo <= 0)   tertiaryMagazineIsEmpty = true;
            if (tertiaryCurrentAmmo > 0)    tertiaryMagazineIsEmpty = false;

            if (tertiaryMagazineIsEmpty)
            {
                canShoot = false;
                canReload = true;
            }
            else    canShoot = true;
        }        
    }

    public bool AllHasMaxAmmo(int i)
    {
        if(i == 0)
        {
            if (primaryCurrentAmmo != inventory.weapons[0].magazineSize || primaryCurrentAmmoStorage != inventory.weapons[0].storedAmmo)        return false;
            else    return true;
        }
        if (i == 1)
        {
            if (secondaryCurrentAmmo != inventory.weapons[1].magazineSize || secondaryCurrentAmmoStorage != inventory.weapons[1].storedAmmo)    return false;
            else    return true;
        }
        if (i == 2)
        {
            if (tertiaryCurrentAmmo != inventory.weapons[2].magazineSize || tertiaryCurrentAmmoStorage != inventory.weapons[2].storedAmmo)      return false;
            else    return true;
        }
        else    return false;
    }

    public int ReturnCurrentAmmo(int index)
    {
        int currentAmmo = 0;

        if (index == 0)     currentAmmo = primaryCurrentAmmo;
        if (index == 1)     currentAmmo = secondaryCurrentAmmo;
        if (index == 2)     currentAmmo = tertiaryCurrentAmmo;

        return currentAmmo;
    }
    public int ReturnStorageAmmo(int index)
    {
        int storageAmmo = 0;

        if (index == 0)     storageAmmo = primaryCurrentAmmoStorage;
        if (index == 1)     storageAmmo = secondaryCurrentAmmoStorage;
        if (index == 2)     storageAmmo = tertiaryCurrentAmmoStorage;

        return storageAmmo;
    }
    public bool WeaponHasMaxAmmo(int index)
    {
        if (inventory.weapons[index] == null)   return true;   // If there's no weapon in inventory, don't go further...

        if (index == 0)     return PrimaryHasMaxAmmo();
        if (index == 1)     return SecondaryHasMaxAmmo();
        if (index == 2)     return TertiaryHasMaxAmmo();

        else    return true;
    }
    public bool WeaponHasMaxAmmo(int index, int magazineAmmo, int storageAmmo)
    {
        if (inventory.weapons[index] == null)   return true;  // If there's no weapon in inventory, don't go further...
        if (magazineAmmo != inventory.weapons[index].magazineSize || storageAmmo != inventory.weapons[index].storedAmmo)    return false;
        else    return true;
    }
    // Check if there's max ammo in current mag but also storage
    public bool PrimaryHasMaxAmmoMagNStorage()
    {
        if (inventory.weapons[0] == null)   return true; // If there's no weapon in inventory, don't go further...  
        if (primaryCurrentAmmo != inventory.weapons[0].magazineSize || primaryCurrentAmmoStorage != inventory.weapons[0].storedAmmo)    return false;
        else    return true;
    }
    public bool SecondaryHasMaxAmmoMagNStorage()
    {
        if (inventory.weapons[1] == null)   return true;
        if (secondaryCurrentAmmo != inventory.weapons[1].magazineSize || secondaryCurrentAmmoStorage != inventory.weapons[1].storedAmmo)    return false;
        else    return true;
    }
    public bool TertiaryHasMaxAmmoMagNStorage()
    {
        if (inventory.weapons[2] == null)   return true;
        if (tertiaryCurrentAmmo != inventory.weapons[2].magazineSize || tertiaryCurrentAmmoStorage != inventory.weapons[2].storedAmmo)   return false;
        else    return true;
    }
    // Check if there's max ammo in current mag only
    public bool PrimaryHasMaxAmmo()
    {
        if (inventory.weapons[0] == null)   return true;  // If there's no weapon in inventory, don't go further...
        if (primaryCurrentAmmo != inventory.weapons[0].magazineSize)    return false;
        else    return true;
    }
    public bool SecondaryHasMaxAmmo()
    {
        if (inventory.weapons[1] == null)   return true;
        if (secondaryCurrentAmmo != inventory.weapons[1].magazineSize)   return false;
        else    return true;
    }
    public bool TertiaryHasMaxAmmo()
    {
        if (inventory.weapons[2] == null)   return true;
        if (tertiaryCurrentAmmo != inventory.weapons[2].magazineSize)   return false;
        else    return true;
    }

    void UseAmmo(int slot, int currentAmmoUsed, int currentStoredAmmoUsed)
    {
        if (slot == 0) // Primary
        {
            if (primaryCurrentAmmo <= 0)
            {
                primaryMagazineIsEmpty = true;
                CheckCanShoot(equipmentManager.currentlyEquippedWeapon);
            }
            else
            {
                primaryCurrentAmmo -= currentAmmoUsed;
                primaryCurrentAmmoStorage -= currentStoredAmmoUsed;
                hud.UpdateWeaponAmmoUI(primaryCurrentAmmo, primaryCurrentAmmoStorage);
            }
        }
        if (slot == 1) // Secondary
        {
            if (secondaryCurrentAmmo <= 0)
            {
                secondaryMagazineIsEmpty = true;
                CheckCanShoot(equipmentManager.currentlyEquippedWeapon);
            }
            else
            {
                secondaryCurrentAmmo -= currentAmmoUsed;
                secondaryCurrentAmmoStorage -= currentStoredAmmoUsed;
                hud.UpdateWeaponAmmoUI(secondaryCurrentAmmo, secondaryCurrentAmmoStorage);
            }
        }
        if (slot == 2) // Tertiary
        {
            if (tertiaryCurrentAmmo <= 0)
            {
                tertiaryMagazineIsEmpty = true;
                CheckCanShoot(equipmentManager.currentlyEquippedWeapon);
            }
            else
            {
                tertiaryCurrentAmmo -= currentAmmoUsed;
                tertiaryCurrentAmmoStorage -= currentStoredAmmoUsed;
                hud.UpdateWeaponAmmoUI(tertiaryCurrentAmmo, tertiaryCurrentAmmoStorage);
            }
        }
        canReload = true;
    }

    void Reload(int slot)
    {
        Weapon weapon = inventory.weapons[slot];
        bool hasAmmoToLoad = weapon != null && MagazineMath.CanReload(new AmmoState(ReturnCurrentAmmo(slot), ReturnStorageAmmo(slot)), weapon.magazineSize);

        if (!stats.IsDead() && !uiManager.GameIsPaused() && canReload && hasAmmoToLoad && melee.GetCanMelee())
            StartCoroutine(PlayReloadAnimation(slot));
    }
    float CalculateReloadAnimationSpeed()
    {
        if (PerkManager.flyingHandActive)   return perkManager.GetReloadMultiplier();   // Perk to reload faster
        else                                return anim.GetCurrentAnimatorStateInfo(0).length;
    }
    public IEnumerator PlayReloadAnimation(int slot)
    {
        float animationSpeed = CalculateReloadAnimationSpeed(); // Anim speed depending on if player has perk or not

        anim.SetBool("Reloading", true);    // Reload gun
        canReload = false;  // Weapon is reloading, so it can't reload again until it finishes
        canShoot = false;

        yield return new WaitForSeconds(animationSpeed);

        anim.SetBool("Reloading", false);   // Put weapon back to functionality
        sfxManager.PlaySound(inventory.weapons[slot].reloadSound);

        yield return new WaitForSeconds(animationSpeed);    // Let the weapon return to its idle pose

        ReloadLogic(slot);
        canReload = true;
        canShoot = true;
    }
    void ReloadLogic(int slot)
    {
        Weapon weapon = inventory.weapons[slot];
        AmmoState reloaded = MagazineMath.Reload(new AmmoState(ReturnCurrentAmmo(slot), ReturnStorageAmmo(slot)), weapon.magazineSize);
        SetAmmo(slot, reloaded);
        hud.UpdateWeaponAmmoUI(reloaded.Magazine, reloaded.Reserve);
    }

    void SetAmmo(int slot, AmmoState ammo)
    {
        bool empty = ammo.Magazine <= 0;
        if (slot == 0) { primaryCurrentAmmo = ammo.Magazine;   primaryCurrentAmmoStorage = ammo.Reserve;   primaryMagazineIsEmpty = empty; }
        if (slot == 1) { secondaryCurrentAmmo = ammo.Magazine; secondaryCurrentAmmoStorage = ammo.Reserve; secondaryMagazineIsEmpty = empty; }
        if (slot == 2) { tertiaryCurrentAmmo = ammo.Magazine;  tertiaryCurrentAmmoStorage = ammo.Reserve;  tertiaryMagazineIsEmpty = empty; }
    }

    void PlayShootAnimations()
    {
        equipmentManager.currentWeaponAnim.SetTrigger("shoot");
    }

    public int ReturnCurrentAmmoStats()
    {
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 0)     return primaryCurrentAmmo;
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 1)     return secondaryCurrentAmmo;
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 2)     return tertiaryCurrentAmmo;
        else    return 0;
    }
    public int ReturnCurrentAmmoStorageStats()
    {
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 0)     return primaryCurrentAmmoStorage;
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 1)     return secondaryCurrentAmmoStorage;
        if (equipmentManager.GetCurrentlyEquippedWeapon() == 2)     return tertiaryCurrentAmmoStorage;
        else    return 0;
    }

    public void RefillAllAmmo() // Used mainly for maxAmmo PowerUp!
    {
        // Fill all guns
        if (inventory.inventorySpace == 1)
        {
            if (inventory.weapons[0] != null)
            {
                primaryCurrentAmmo = inventory.weapons[0].magazineSize;
                primaryCurrentAmmoStorage = inventory.weapons[0].storedAmmo;
                primaryMagazineIsEmpty = false;
            }
        }
        if (inventory.inventorySpace == 2)
        {
            if (inventory.weapons[0] != null)
            {
                primaryCurrentAmmo = inventory.weapons[0].magazineSize;
                primaryCurrentAmmoStorage = inventory.weapons[0].storedAmmo;
                primaryMagazineIsEmpty = false;
            }
            if (inventory.weapons[1] != null)
            {
                secondaryCurrentAmmo = inventory.weapons[1].magazineSize;
                secondaryCurrentAmmoStorage = inventory.weapons[1].storedAmmo;
                secondaryMagazineIsEmpty = false;
            }
        }
        if (inventory.inventorySpace == 3)
        {
            if (inventory.weapons[0] != null)
            {
                primaryCurrentAmmo = inventory.weapons[0].magazineSize;
                primaryCurrentAmmoStorage = inventory.weapons[0].storedAmmo;
                primaryMagazineIsEmpty = false;
            }
            if (inventory.weapons[1] != null)
            {
                secondaryCurrentAmmo = inventory.weapons[1].magazineSize;
                secondaryCurrentAmmoStorage = inventory.weapons[1].storedAmmo;
                secondaryMagazineIsEmpty = false;
            }
            if (inventory.weapons[2] != null)
            {
                tertiaryCurrentAmmo = inventory.weapons[2].magazineSize;
                tertiaryCurrentAmmoStorage = inventory.weapons[2].storedAmmo;
                tertiaryMagazineIsEmpty = false;
            }
        }

        // Update current gun UI (Show new bullet quantity!)
        if(equipmentManager.currentlyEquippedWeapon == 0)       hud.UpdateWeaponAmmoUI(primaryCurrentAmmo, primaryCurrentAmmoStorage);
        if (equipmentManager.currentlyEquippedWeapon == 1)      hud.UpdateWeaponAmmoUI(secondaryCurrentAmmo, secondaryCurrentAmmoStorage);
        if (equipmentManager.currentlyEquippedWeapon == 2)      hud.UpdateWeaponAmmoUI(tertiaryCurrentAmmo, tertiaryCurrentAmmoStorage);
    }

    #endregion

    #region - Reference && Initialize -

    void GetReferences()
    {
        cam = GetComponentInChildren<Camera>();
        inventory = GetComponent<Inventory>();
        equipmentManager = GetComponent<EquipmentManager>();
        hud = GetComponent<PlayerHUD>();
        weaponDecals = GetComponentInChildren<WeaponDecals>();
        cameraRecoil = GetComponentInChildren<CameraRecoil>();
        shakeHolderTransform = GetComponentInChildren<ShakeTransform>();
        stats = GetComponent<PlayerStats>();
        uiManager = GetComponentInChildren<UIManager>();
        perkManager = GetComponent<PerkManager>();
        statsManager = GetComponentInChildren<StatsManager>();
        melee = GetComponentInChildren<Melee>();
    }
    void Initialize()
    {
        playerInput = GameInput.CreateControls(this); // Initialize Player input to use new input system
        if (!playerInput.asset.enabled) playerInput.Enable(); // Enable input system!

        playerInput.Weapon.Fire1Pressed.performed += e => StartShooting();     // Shoot
        playerInput.Weapon.Fire1Pressed.canceled += e => StopShooting();     // Shoot
        playerInput.Weapon.Reload.performed += e => Reload(equipmentManager.currentlyEquippedWeapon);   // Reload Weapon
        canShoot = true;
        canReload = true;
        shootingAvoidLayers = ~shootingAvoidLayers; // Layermask (to avoid "Player" layermask)
        canPlay_EmptyMagSound = true;
    }
    void OnDisable()
    {
        playerInput.Disable();
    }

    public void InitAmmo(int slot, Weapon weapon)
    {
        if (slot == 0) // Primary
        {
            primaryCurrentAmmo = weapon.magazineSize;
            primaryCurrentAmmoStorage = weapon.storedAmmo;
            primaryMagazineIsEmpty = false;
        }

        if (slot == 1) // Secondary
        {
            secondaryCurrentAmmo = weapon.magazineSize;
            secondaryCurrentAmmoStorage = weapon.storedAmmo;
            secondaryMagazineIsEmpty = false;
        }

        if (slot == 2) // Tertiary
        {
            tertiaryCurrentAmmo = weapon.magazineSize;
            tertiaryCurrentAmmoStorage = weapon.storedAmmo;
            tertiaryMagazineIsEmpty = false;
        }
    }

    #endregion

}
