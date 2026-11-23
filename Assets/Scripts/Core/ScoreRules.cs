using System;

namespace ZBattleYard.Core
{
    /// <summary>Points, purchases and loot rules.</summary>
    public static class ScoreRules
    {
        /// <summary>Points actually awarded, doubled while the Double Points power-up is active.</summary>
        public static int AwardedPoints(int basePoints, bool doublePointsActive) =>
            doublePointsActive ? basePoints * 2 : basePoints;

        /// <summary>True if the player can pay <paramref name="price"/>.</summary>
        public static bool CanAfford(int currentPoints, int price) => price >= 0 && currentPoints >= price;

        /// <summary>
        /// Decides whether an enemy drops a power-up.
        /// <paramref name="roll"/> is a uniform random integer in [0, 100); the drop happens with
        /// exactly <paramref name="dropChancePercent"/>% probability.
        /// </summary>
        public static bool ShouldDropLoot(int roll, int dropChancePercent)
        {
            if (roll < 0 || roll >= 100) throw new ArgumentOutOfRangeException(nameof(roll));
            return roll < dropChancePercent;
        }
    }
}
