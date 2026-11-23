using System;
using NUnit.Framework;
using ZBattleYard.Core;

namespace ZBattleYard.Tests.Core
{
    public class WaveRulesTests
    {
        [Test]
        public void FirstWave_SpawnsTheBaseAmount()
        {
            Assert.AreEqual(6, WaveRules.EnemiesInWave(0, firstWaveEnemies: 6, players: 1, maxEnemiesPerWave: 150));
        }

        [TestCase(1, 8)]    // 6 * 1 * 1.25 = 7.5 -> 8 (round half to even, like Mathf.RoundToInt)
        [TestCase(2, 15)]   // 15
        [TestCase(3, 22)]   // 22.5 -> 22
        [TestCase(10, 75)]
        public void LaterWaves_GrowLinearly(int waveIndex, int expected)
        {
            Assert.AreEqual(expected, WaveRules.EnemiesInWave(waveIndex, 6, 1, 150));
        }

        [Test]
        public void EnemiesPerWave_AreCapped()
        {
            Assert.AreEqual(150, WaveRules.EnemiesInWave(500, 6, 1, 150));
        }

        [Test]
        public void EnemiesPerWave_ScaleWithPlayers()
        {
            Assert.AreEqual(12, WaveRules.EnemiesInWave(0, 6, 2, 150));
            Assert.AreEqual(30, WaveRules.EnemiesInWave(2, 6, 2, 150));
        }

        [Test]
        public void EnemiesPerWave_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => WaveRules.EnemiesInWave(-1, 6, 1, 150));
            Assert.Throws<ArgumentOutOfRangeException>(() => WaveRules.EnemiesInWave(0, 6, 0, 150));
        }

        [TestCase(0, 100)]
        [TestCase(4, 500)]
        public void ZombieHealth_GrowsEveryWave(int waveIndex, int expected)
        {
            Assert.AreEqual(expected, WaveRules.ZombieHealth(100, waveIndex, 1f));
        }

        [Test]
        public void ZombieSpeed_IsCappedAtMaxSpeed()
        {
            Assert.AreEqual(2f, WaveRules.ZombieSpeed(2f, 0, 1f, 5f), 1e-5);
            Assert.AreEqual(4f, WaveRules.ZombieSpeed(2f, 1, 1f, 5f), 1e-5);
            Assert.AreEqual(5f, WaveRules.ZombieSpeed(2f, 9, 1f, 5f), 1e-5);
        }

        [Test]
        public void NextWaveIndex_WrapsAround()
        {
            Assert.AreEqual(1, WaveRules.NextWaveIndex(0, 999));
            Assert.AreEqual(0, WaveRules.NextWaveIndex(998, 999));
            Assert.AreEqual(0, WaveRules.NextWaveIndex(3, 0));
        }

        [TestCase(2, 2, 4, 4)]
        [TestCase(3, 2, 4, 4)]
        [TestCase(4, 2, 4, 4)]
        [TestCase(0, 2, 4, 2)]
        [TestCase(5, 2, 4, 5)]   // Never removes grenades the player already has
        public void RefillGrenades_RespectsTheLimit(int current, int refill, int max, int expected)
        {
            Assert.AreEqual(expected, WaveRules.RefillGrenades(current, refill, max));
        }
    }
}
