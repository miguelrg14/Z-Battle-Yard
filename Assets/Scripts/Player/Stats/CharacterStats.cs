using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStats : MonoBehaviour
{
    [SerializeField] protected int health;
    [SerializeField] protected int maxHealth;
    [SerializeField] protected float stamina;
    [SerializeField] protected float maxStamina;
    [SerializeField] protected bool isDead;
    protected bool initHealthRegenValue;

    void Start() => InitVariables();

    #region - Logic -

    public virtual void CheckHealth()
    {
        if (health <= 0)
        {
            health = 0;
            if (!isDead)    // Die only once, even if several hits land in the same frame
                Die();
        }
        if (health >= maxHealth)
        {
            health = maxHealth;
        }
    }
    public virtual void Die()
    {
        EnemySpawner.enemyDied_RoundCheck = true;
        isDead = true;
    }
    public bool IsDead() => isDead;
    public int GetHealth() => health;

    public void SetHealthTo(int healthToSetTo)
    {
        health = healthToSetTo;
        CheckHealth();
    }
    public virtual void TakeDamage(int damage)
    {
        int healthAfterDamage = health - damage;
        SetHealthTo(healthAfterDamage);
        if (CompareTag("Player"))
            PlayerStats.healthResetCooldown = true;
        initHealthRegenValue = true;
    }
    public void InstantKill()
    {
        if (isDead)
            return;
        health = 0;
        Die();
    }
    public void Heal(int heal)
    {
        // The old version multiplied by (int)Time.deltaTime, which is always 0, so medkits did nothing
        SetHealthTo(health + heal);
    }

    public int GetMaxHealth() => maxHealth;

    /// Stamina
    public virtual void CheckStamina()
    {
        if (stamina <= 0)
        {
            stamina = 0;
            return;
        }
        if (stamina >= maxStamina)
        {
            stamina = maxStamina;
        }
    }
    public virtual void UseStamina(float amount) => stamina -= amount * Time.deltaTime;

    public virtual void InitVariables()
    {
        ResetStats();
        isDead = false;
    }

    protected void ResetStats()
    {
        SetHealthTo(maxHealth);        
        stamina = maxStamina;
    }

    #endregion
}
