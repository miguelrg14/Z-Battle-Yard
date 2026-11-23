using NUnit.Framework;
using UnityEngine;

namespace ZBattleYard.Tests.PlayMode
{
    public class CharacterStatsTests
    {
        GameObject gameObject;
        TestCharacterStats stats;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("Stats");
            stats = gameObject.AddComponent<TestCharacterStats>();
            stats.Setup(100);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(gameObject);

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            stats.TakeDamage(30);

            Assert.AreEqual(70, stats.GetHealth());
            Assert.IsFalse(stats.IsDead());
        }

        [Test]
        public void LethalHits_KillOnlyOnce()
        {
            stats.TakeDamage(150);
            stats.TakeDamage(150);
            stats.InstantKill();

            Assert.IsTrue(stats.IsDead());
            Assert.AreEqual(0, stats.GetHealth());
            Assert.AreEqual(1, stats.Deaths, "Die() must run once, otherwise kills and drops are counted twice");
        }

        [Test]
        public void InstantKill_KillsAtFullHealth()
        {
            stats.InstantKill();

            Assert.IsTrue(stats.IsDead());
            Assert.AreEqual(1, stats.Deaths);
        }

        [Test]
        public void Heal_RestoresHealthUpToMax()
        {
            stats.TakeDamage(60);
            stats.Heal(20);
            Assert.AreEqual(60, stats.GetHealth());

            stats.Heal(1000);
            Assert.AreEqual(100, stats.GetHealth());
        }
    }
}
