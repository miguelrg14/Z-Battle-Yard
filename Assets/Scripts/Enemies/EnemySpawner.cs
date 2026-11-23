using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZBattleYard.Core;
using UnityEngine.UI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] SFXManager sfxManager;
    StatsManager statsManager;
    GrenadeLogic grenadeLogic;

    [SerializeField] AudioClip gameStart;
    [SerializeField] AudioClip waveComplete;

    public enum SpawnState { SPAWNING, WAITING, COUNTING };

    [SerializeField] bool startGame = true;
    [Header("Wave Properties")]
    [SerializeField] int maximumWavesNumber = 999;
    [SerializeField] float timeBetweenWaves = 5f;
    float waveCountdown = 0f;
    [SerializeField] Transform[] spawners;
    [SerializeField] List<Transform> activeSpawners;

    [Header("Player rewards")]
    [SerializeField] int newGrenadesAmountPerRound = 2;

    [Header("Round Entities")]
    [SerializeField] int firstRoundZombiesToScale = 6;
    [SerializeField] int maximumRoundZombiesOnMap = 30;
    [SerializeField] int maximumRoundZombies = 150;
    [SerializeField] GameObject zombie;
    [SerializeField] public List<CharacterStats> enemyList; // Stores enemies in round
    [SerializeField] List<Wave> waves;

    /// Canvas
    [Header("Canvas")]
    [Header("Round")]
    [SerializeField] TextMeshProUGUI waveText;
    [Header("CountDown")]
    [SerializeField] CanvasGroup countDownCanvas;
    [SerializeField] TextMeshProUGUI countdownTimerText;

    SpawnState state = SpawnState.COUNTING;
    public static int currentWave;    // Current wave based in array position
    int roundCount = 0; // Current wave based in HUD round.text

    int playersPlaying = 1;
    public static bool enemyDied_RoundCheck;
    float spawnDelayTimer;

    void Awake() => GetReferences();
    void Start()
    {
        InitializeVariables();
        InitializeRoundsInfo();
        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] == null)
                startGame = false;
        }
        
        UpdateActiveEnemySpawns();
    }

    public void UpdateActiveEnemySpawns()
    {
        if (activeSpawners.Count == 0)
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i].gameObject.activeSelf)
                    activeSpawners.Add(spawners[i]);
            }
        }
        else
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i].gameObject.activeSelf && !activeSpawners.Contains(spawners[i]))
                    activeSpawners.Add(spawners[i]);          
            }
        }
    }
    void Update()
    {
        if(startGame)
        {
            ShowNewRoundCanvas();
            if (enemyDied_RoundCheck)
                CheckAndRemoveIfEnemyInListIsDead();

            if (state == SpawnState.WAITING) // Check if all enemies are dead
            {
                if (!EnemiesAreDead())  return;
                else                    CompleteWave();
            }

            if (waveCountdown <= 0)
            {
                SpawnWave(waves[currentWave]);
                spawnDelayTimer -= Time.deltaTime;
            }
            else
            {
                // The countdown itself is advanced in ShowNewRoundCanvas (it used to be decreased twice per frame)
                countdownTimerText.text = Mathf.CeilToInt(waveCountdown).ToString();
            }
        }
    }

    void CheckAndRemoveIfEnemyInListIsDead()
    {
        // Destroyed enemies compare equal to null in Unity. Removing while iterating forwards skipped elements.
        enemyList.RemoveAll(enemy => enemy == null);
        enemyDied_RoundCheck = false;
    }
    void ShowNewRoundCanvas()
    {
        if (waveCountdown > 0)          // Show countdown in the end of round!
        {
            waveCountdown -= Time.deltaTime;
            countDownCanvas.alpha = 1; // Show Canvas
        }
        if (waveCountdown <= 0)
        {
            waveCountdown = 0;
            countDownCanvas.alpha = 0; // Hide Canvas
        }
    }

    void SpawnWave(Wave wave)
    {
        state = SpawnState.SPAWNING;

        if (CanSpawn(waves[currentWave]))   // Spawn enemies
        {
            SpawnZombie(wave.enemy);
            spawnDelayTimer = wave.spawnDelay;
            return;
        }

        if (wave.enemyAmount == 0)
            state = SpawnState.WAITING;
    }
    bool CanSpawn(Wave wave) // SpawnDelay
    {
        if (enemyList.Count < maximumRoundZombiesOnMap && wave.enemyAmount > 0 && spawnDelayTimer <= 0)
            return true;

        return false;
    }
    void SpawnZombie(GameObject enemy)
    {
        // Pick a random spawner among the active ones (the old loop never ended if none was active)
        activeSpawners.RemoveAll(spawner => spawner == null || !spawner.gameObject.activeSelf);
        if (activeSpawners.Count == 0)
            UpdateActiveEnemySpawns();
        if (activeSpawners.Count == 0)
        {
            Debug.LogWarning("EnemySpawner: no active spawn points, skipping spawn.", this);
            return;
        }
        Transform randomSpawner = activeSpawners[UnityEngine.Random.Range(0, activeSpawners.Count)];
        waves[currentWave].enemyAmount--;

        GameObject newEnemy = Instantiate(enemy, randomSpawner.position, randomSpawner.rotation);
        CharacterStats newEnemyStats = newEnemy.GetComponent<CharacterStats>();
        enemyList.Add(newEnemyStats);
        SmokeTestMode.Log($"Zombie spawned (wave {roundCount + 1}, {enemyList.Count} alive)");
    }

    bool EnemiesAreDead() // Check if enemies are dead
    {
        foreach (CharacterStats enemy in enemyList)
        {
            if (enemy != null && !enemy.IsDead())
                return false;
        }
        return true;
    }
    void CompleteWave() // Logic when waves are completed
    {
        enemyList.Clear();
        roundCount++;
        waveText.text = (roundCount + 1).ToString();
        state = SpawnState.COUNTING;
        waveCountdown = timeBetweenWaves;

        currentWave = WaveRules.NextWaveIndex(currentWave, waves.Count);

        statsManager.UpdateTabbingStats();
        grenadeLogic.AddNewRoundGrenades(newGrenadesAmountPerRound);
        SmokeTestMode.Log($"Wave {roundCount} completed");
        sfxManager.PlaySound(waveComplete);
    }

    public int GetRoundNumb_UI()   =>  (roundCount + 1);

    void GetReferences()
    {
        statsManager = GetComponent<StatsManager>();
        grenadeLogic = GetComponentInParent<GrenadeLogic>();
    }
    void InitializeVariables()
    {
        enemyList.Clear();
        waveCountdown = timeBetweenWaves;
        currentWave = 0;
        waveText.text = (roundCount + 1).ToString();
    }
    void InitializeRoundsInfo()
    {
        waves.Clear();
        // First wave (built separately because its enemy amount is not scaled)
        waves.Add(new Wave()
        {
            name = "Wave " + 0,
            enemyAmount = EnemyAmountRound(0),
            spawnDelay = 1f,
            enemy = zombie
        }
        );
        // Rest of rounds
        for (int i = 1; i < maximumWavesNumber; i++)
        {
            waves.Add(new Wave() 
            { 
                name = "Wave " + (i + 1),
                enemyAmount = EnemyAmountRound(i),
                spawnDelay = 1f,
                enemy = zombie
            }
            );
        }
    }

    int EnemyAmountRound(int roundIndex) =>
        WaveRules.EnemiesInWave(roundIndex, firstRoundZombiesToScale, playersPlaying, maximumRoundZombies);
}