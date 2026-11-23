using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    [SerializeField] WeaponDecals weaponDecals;
    PerkManager perkManager;

    [SerializeField] int damage = 10;
    [SerializeField] float impactForce = 30f;
    [SerializeField] float batRadius = 1f;

    void Awake()    => GetReferences(); 
    void OnTriggerEnter(Collider hit)
    {
        Collider[] collidersToMove = Physics.OverlapSphere(transform.position, batRadius); // Get all the nearby objects
        foreach (Collider nearbyObject in collidersToMove)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>(); // Search for rigidbodies 
            if (rb != null) // If object has a rigidbody
                rb.AddExplosionForce(impactForce, transform.position, batRadius); // Add a force to the object to pull it away from the grenade
        }            
        Damage(hit);
        Decals(hit); // Particles through colliders
    }

    void Damage(Collider hit)
    {
        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyBody") || hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyHead"))
        {
            CharacterStats enemyStats = hit.transform.GetComponentInParent<CharacterStats>();
            if (enemyStats.IsDead())
                return;

            if (PowerUpManager.instaKillActive) enemyStats.InstantKill();
            else    // Normal damage
            {
                if (PerkManager.strikeActive)   enemyStats.TakeDamage(damage * perkManager.GetGoldenStrikeMultiplier());
                else                            enemyStats.TakeDamage(damage);
            }
            PlayerHUD.instance.UpdateScoreHitAmount();
        }
    }

    void Decals(Collider hit)
    {
        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyBody"))
        {
            GameObject impactGO = Instantiate(weaponDecals.impactEnemyFrontSplash, new Vector3(hit.transform.position.x, transform.position.y, transform.position.z), hit.transform.rotation); // front blood
            GameObject impactGO2 = Instantiate(weaponDecals.impactEnemyBackSplash, new Vector3(hit.transform.position.x, transform.position.y, transform.position.z), hit.transform.rotation); // Back blood
            Destroy(impactGO, 1f);
            Destroy(impactGO2, 2f);
        }
        if (hit.transform.gameObject.layer == LayerMask.NameToLayer("EnemyHead"))
        {
            GameObject impactGO = Instantiate(weaponDecals.impactEnemyFrontSplash, new Vector3(hit.transform.position.x, transform.position.y, transform.position.z), hit.transform.rotation); // front blood
            GameObject impactGO2 = Instantiate(weaponDecals.impactEnemyBackSplash, new Vector3(hit.transform.position.x, transform.position.y, transform.position.z), hit.transform.rotation); // Back blood
            Destroy(impactGO, 1f);
            Destroy(impactGO2, 2f);
        }
        //// Extras...
    }

    void GetReferences()
    {
        perkManager = GetComponentInParent<PerkManager>();
    }
}

