using System.Collections;
using UnityEngine;

public class Melee : MonoBehaviour
{
    GameControls playerInput;
    [SerializeField] WeaponAnimations weaponAnimations;
    WeaponShooting weaponShooting;
    Animator meleeAnimator;

    [Header("Melee Attack")]
    [SerializeField] GameObject meleeWeapon;
    [SerializeField] float cooldown;
    [SerializeField] AnimationClip attackClip;
    [SerializeField] Vector3 rotation;
    Vector3 initialRotation;

    bool canMelee = true;

    void Awake() => GetReferences();
    void Start() => Initialize();

    void CalculateMelee()
    {
        if (canMelee)   MeleeAttack();
    }

    public bool GetCanMelee() => canMelee;
    public void MeleeAttack()
    {
        if (!weaponShooting.canReload)
            return;

        canMelee = false;
        weaponShooting.canReload = false;
        weaponShooting.canShoot = false;
        meleeWeapon.SetActive(true);
        weaponAnimations.weaponAnimator.SetTrigger("MeleeAttack"); // WeaponController Animation
        meleeAnimator.SetTrigger("Attack"); // Internal Animation
        StartCoroutine(Activation());
    }
    IEnumerator Activation()
    {
        yield return new WaitForSeconds(attackClip.length);
        meleeWeapon.SetActive(false);
        meleeWeapon.transform.Rotate(0, 90, 0, Space.Self);
        yield return new WaitForSeconds(cooldown);
        canMelee = true;
        weaponShooting.canReload = true;
        weaponShooting.canShoot = true;
    }

    void GetReferences()
    {
        meleeAnimator = gameObject.GetComponent<Animator>(); // Animator
        weaponShooting = GetComponentInParent<WeaponShooting>();
    }
    void Initialize()
    {
        playerInput = GameInput.CreateControls(this); // Initialize New input system
        playerInput.Enable();
        playerInput.Weapon.Melee.performed += e => CalculateMelee(); // Melee
        meleeWeapon.SetActive(false);
    }
    void OnDisable()
    {
        playerInput.Disable();
    }
}
