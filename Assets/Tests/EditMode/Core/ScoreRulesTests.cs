using System;
using NUnit.Framework;
using ZBattleYard.Core;

namespace ZBattleYard.Tests.Core
{
    public class ScoreRulesTests
    {
        [Test]
        public void DoublePoints_DoublesTheReward()
        {
            Assert.AreEqual(100, ScoreRules.AwardedPoints(100, doublePointsActive: false));
            Assert.AreEqual(200, ScoreRules.AwardedPoints(100, doublePointsActive: true));
        }

        [TestCase(950, 950, true)]
        [TestCase(949, 950, false)]
        [TestCase(0, 0, true)]
        [TestCase(10, -1, false)]
        public void CanAfford(int points, int price, bool expected)
        {
            Assert.AreEqual(expected, ScoreRules.CanAfford(points, price));
        }

        [Test]
        public void LootChance_DropsExactlyTheConfiguredPercentage()
        {
            int drops = 0;
            for (int roll = 0; roll < 100; roll++)
                if (ScoreRules.ShouldDropLoot(roll, dropChancePercent: 2))
                    drops++;

            Assert.AreEqual(2, drops);
        }

        [Test]
        public void LootChance_ZeroNeverDrops_HundredAlwaysDrops()
        {
            for (int roll = 0; roll < 100; roll++)
            {
                Assert.IsFalse(ScoreRules.ShouldDropLoot(roll, 0));
                Assert.IsTrue(ScoreRules.ShouldDropLoot(roll, 100));
            }
        }

        [Test]
        public void LootChance_RejectsRollsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreRules.ShouldDropLoot(100, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => ScoreRules.ShouldDropLoot(-1, 2));
        }
    }
}
