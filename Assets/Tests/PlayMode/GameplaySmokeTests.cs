using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ZBattleYard.Tests.PlayMode
{
    /// <summary>
    /// End-to-end checks that load the real scenes. Any exception or error log during a test
    /// makes it fail, so these also catch missing references and broken scene setup.
    /// </summary>
    public class GameplaySmokeTests
    {
        const string MainMenuScene = "MainMenu";
        const string GameScene = "TrainStation";
        const float WaveTimeout = 60f;

        [UnitySetUp]
        public IEnumerator ResetState()
        {
            Time.timeScale = 1f;
            PowerUpManager.doublePointsActive = false;
            PowerUpManager.instaKillActive = false;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreTimeScale()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_LoadsWithoutErrors()
        {
            yield return LoadScene(MainMenuScene);
            yield return new WaitForSeconds(1f);

            Assert.IsNotNull(Object.FindAnyObjectByType<MainMenuManager>(), "Main menu manager not found");
        }

        [UnityTest]
        public IEnumerator TrainStation_LoadsPlayerWithDefaultWeapon()
        {
            yield return LoadScene(GameScene);
            yield return new WaitForSeconds(1f);

            var player = Object.FindAnyObjectByType<PlayerStats>();
            Assert.IsNotNull(player, "Player not found");
            Assert.IsFalse(player.IsDead());
            Assert.Greater(player.GetHealth(), 0);

            var inventory = Object.FindAnyObjectByType<Inventory>();
            Assert.IsNotNull(inventory, "Inventory not found");
            Assert.IsNotNull(inventory.weapons[inventory.WeaponInInventoryIndex(Object.FindAnyObjectByType<EquipmentManager>().defaultWeapon)],
                "The default weapon was not added to the inventory");

            Assert.IsNotNull(PlayerTarget.instance, "Enemies have no target to chase");
        }

        [UnityTest]
        public IEnumerator TrainStation_FirstWaveSpawns_AndKillsAwardPoints()
        {
            yield return LoadScene(GameScene);

            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            var score = Object.FindAnyObjectByType<Score>();
            Assert.IsNotNull(spawner, "EnemySpawner not found");
            Assert.IsNotNull(score, "Score not found");

            // Speed up the countdown before the first wave
            Time.timeScale = 4f;
            float waited = 0f;
            while (spawner.enemyList.Count == 0 && waited < WaveTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Time.timeScale = 1f;

            Assert.Greater(spawner.enemyList.Count, 0, "No zombie spawned within " + WaveTimeout + " seconds");

            CharacterStats zombie = spawner.enemyList[0];
            int scoreBefore = score.currentScore;
            int killsBefore = StatsManager.kills;

            zombie.InstantKill();
            yield return null;

            Assert.IsTrue(zombie.IsDead());
            Assert.AreEqual(scoreBefore + score.scoreAmountOnKill, score.currentScore, "A kill must award the kill points once");
            Assert.AreEqual(killsBefore + 1, StatsManager.kills, "A kill must be counted once");
        }

        static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!load.isDone)
                yield return null;
            yield return null;  // Let Awake/Start run
        }
    }
}
