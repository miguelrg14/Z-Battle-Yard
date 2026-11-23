using System.Collections.Generic;
using UnityEngine;
using ZBattleYard.Core;
using UnityEngine.AI;

public class ZombieStats : CharacterStats
{
    Animator animator;
    ZombieController zombieController;
    NavMeshAgent agent = null;

    [SerializeField] int damage = 40;
    public float attackSpeed = 0.65f;

    [SerializeField] float destroyTimer_AfterDie = 10f;
    [SerializeField] int baseHealth = 100;
    [SerializeField] [Range(0.5f, 2f)] float healthMultiplierPerRound = 1f;

    [Header("Drops")]
    [SerializeField] PowerUp[] powerUps;
    [SerializeField] [Range(0.0f, 100.0f)] int dropPowerUpProbability = 2;

    int enemyListId;

    void Awake() => GetReferences();
    void Start() => InitVariables();

    public void DealDamage(CharacterStats statsToDamage) => statsToDamage.TakeDamage(damage);   // Damaging functionality

    public override void Die()
    {
        base.Die();
        PlayerHUD.instance.UpdateScoreKillAmount();
        DropPowerUp();
        EnemySpawner.enemyDied_RoundCheck = true;

        animator.enabled = false;   // Ragdoll takes over
        zombieController.enabled = false;
        if (agent.isOnNavMesh)
            agent.isStopped = true;
        Destroy(gameObject, destroyTimer_AfterDie);
    }

    /// <summary>Kill caused by the Nuke power-up. Same rewards as a normal kill.</summary>
    public void NukeDeath()
    {
        if (!IsDead())
            Die();
    }

    void DropPowerUp()
    {
        if (powerUps == null || powerUps.Length == 0)
            return;

        if (ScoreRules.ShouldDropLoot(Random.Range(0, 100), dropPowerUpProbability))
        {
            PowerUp powerUp = powerUps[Random.Range(0, powerUps.Length)];
            GameObject powerUpObject = Instantiate(powerUp.prefab, transform.position + Vector3.up, Quaternion.identity);
            powerUpObject.SetActive(true);
        }
    }

    void GetReferences()
    {
        animator = GetComponent<Animator>();
        zombieController = GetComponent<ZombieController>();
        agent = GetComponent<NavMeshAgent>();
    }
    public override void InitVariables()
    {
        maxHealth = WaveRules.ZombieHealth(baseHealth, EnemySpawner.currentWave, healthMultiplierPerRound);
        SetHealthTo(maxHealth);
        isDead = false;

    }
}
