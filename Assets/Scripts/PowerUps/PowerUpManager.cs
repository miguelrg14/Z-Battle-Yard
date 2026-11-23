using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    WeaponShooting shooting;
    [SerializeField] PowerUpUIManager powerUpUIManager;
    PowerUp powerUpMan;

    /// PowerUps
    [SerializeField] public static bool instaKillActive;
    [SerializeField] public static bool doublePointsActive;
    [SerializeField] public static bool magicBoxSaleActive;
    [SerializeField] public static bool slowTimeActive;

    void Awake()
    {
        GetReferences();
    }
    void Start()
    {
        InitVariables();
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.tag == "PowerUp")     // If player collides/interacts with a "PowerUp", activate it's workage
        {
            GameObject powerUpGameObject = other.gameObject;
            powerUpMan = powerUpGameObject.GetComponent<PowerUpLogic>().powerUp;

            AudioSource.PlayClipAtPoint(powerUpMan.pickupSound, gameObject.transform.position);

            ActivatePowerUpGameChanger();

            Destroy(powerUpGameObject);
            return;
        }
        else
            return;
    }

    #region - Logic -

    void ActivatePowerUpGameChanger()
    {
        PowerType type = powerUpMan.powerType;
        int duration = powerUpMan.duration;

        if (type == PowerType.instaKill)
        {
            ActivateInstaKill(duration);
            powerUpUIManager.AddPowerUpImageFeedback(powerUpMan);   // UI Feedback
        }

        if (type == PowerType.maxAmmo)
        {
            ActivateMaxAmmo();
        }

        if (type == PowerType.doublePoints)
        {
            ActivateDoublePoints(duration);
            powerUpUIManager.AddPowerUpImageFeedback(powerUpMan);
        }

        if (type == PowerType.nuke)
        {
            ActivateNuke();
        }

        if (type == PowerType.magicBoxDiscount)
        {
            ActivateMagicBoxSale(duration);
            powerUpUIManager.AddPowerUpImageFeedback(powerUpMan);
        }

        if (type == PowerType.slowTime)
        {
            ActivateSlowTime(duration);
            powerUpUIManager.AddPowerUpImageFeedback(powerUpMan);
        }

        if (type == PowerType.randomPerk)
        {
            ActivateRandomPerk();
        }

        else
            return;
    }

    #region - PowerUp Activation Methods -

    void ActivateInstaKill(int duration)     // Every shot kills instantly every enemy (for a period of time)
    {
        instaKillActive = true;
    }
    void ActivateMaxAmmo()   // Restores ammo for every gun of the player
    {
        shooting.RefillAllAmmo();
    }
    void ActivateDoublePoints(int duration)  // Player wins double of standard points on every action (for a period of time)
    {
        doublePointsActive = true;
    }
    void ActivateNuke()     // Wipes/Kills every zombie spawned in map instantly
    {
        PlayerHUD.instance.UpdateScoreNukeAmount();

        List<CharacterStats> enemyList = GetComponentInChildren<EnemySpawner>().enemyList;

        // Copy the list: killed enemies can be removed from the spawner's list while we iterate
        foreach (CharacterStats enemy in enemyList.ToArray())
        {
            // Kills are counted by ZombieStats.Die -> Score.AddKillToScore (the old code counted them twice)
            if (enemy is ZombieStats zombie && zombie != null && !zombie.IsDead())
                zombie.NukeDeath();
        }
    }
    void ActivateMagicBoxSale(int duration)  // Magic box is on sale, purchases available for only 10$ (for a period of time)
    {
        magicBoxSaleActive = true;
    }
    void ActivateSlowTime(int duration)  // Enemies move slow (for a period of time)
    {
        slowTimeActive = true;
    }
    void ActivateRandomPerk()    // Gives player a random perk of the ones available in the map
    {
        
    }

    #endregion

    #endregion

    void GetReferences()
    {
        shooting = GetComponent<WeaponShooting>();
    }

    void InitVariables()
    {
        instaKillActive     = false;
        doublePointsActive  = false;
        magicBoxSaleActive  = false;
        slowTimeActive      = false;
    }
}
