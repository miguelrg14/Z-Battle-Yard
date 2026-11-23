using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZBattleYard.Core;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    NavMeshAgent agent = null;
    Animator anim = null;
    ZombieStats stats = null;
    Transform target;
    CharacterStats targetStats;
    ZombieSoundManager zombieSoundManager = null;

    [SerializeField] float rotationSmooth = 4;
    [SerializeField] float baseSpeed;
    [SerializeField] float speedMultiplierPerRound = 1f;
    [SerializeField] float maxSpeed = 5f;
    [SerializeField] float speedVisualizer;

    bool canAttack;
    float timeBetweenAttacks = 1;
    float attackTimer;

    void Awake() => GetReferences();
    void Start() => Initialize();
    void Update()
    {
        CanAttackTimer();
        MoveToTarget();
    }

    #region - Logic -

    void CanAttackTimer()
    {
        if (attackTimer >= timeBetweenAttacks)
        {
            canAttack = true;
        }

        else
        {
            canAttack = false;
            attackTimer += Time.deltaTime;
        }
    }
    void MoveToTarget()
    {
        if (target == null || !agent.isOnNavMesh)
            return;

        agent.SetDestination(target.position);
        anim.SetFloat("Speed", 1f, 0.3f, Time.deltaTime);
        RotateToTarget();

        float distanceToTarget = Vector3.Distance(target.position, transform.position);
        if(distanceToTarget <= agent.stoppingDistance + 0.2f) // Stop zombie when near player ==> Attack
        {
            anim.SetFloat("Speed", 0f);

            if (canAttack && targetStats != null && !targetStats.IsDead())
                AttackTarget(targetStats);
        }
    }

    void RotateToTarget() // Rotate only around the Y axis
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0; // Ignore the height difference so the zombie stays upright
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationSmooth);
    }

    void AttackTarget(CharacterStats statsToDamage)
    {
        anim.SetTrigger("attack");
        stats.DealDamage(statsToDamage);
        attackTimer = 0;
        zombieSoundManager.PlayAttackEffect();
    }

    #endregion

    void GetReferences()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<ZombieStats>();
        zombieSoundManager = GetComponent<ZombieSoundManager>();
    }
    void Initialize()
    {
        target = PlayerTarget.instance;
        if (target != null)
            targetStats = target.GetComponent<CharacterStats>();

        agent.speed = WaveRules.ZombieSpeed(baseSpeed, EnemySpawner.currentWave, speedMultiplierPerRound, maxSpeed);

        speedVisualizer = agent.speed;
        attackTimer = 0;
    }
}
