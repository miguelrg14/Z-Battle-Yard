using System;

namespace ZBattleYard.Core
{
    /// <summary>Difficulty curve of the endless waves: enemy count, health and speed per wave.</summary>
    public static class WaveRules
    {
        /// <summary>
        /// Number of zombies in a wave. <paramref name="waveIndex"/> is zero-based.
        /// The first wave spawns <paramref name="firstWaveEnemies"/>; later waves grow by 25% of that per wave,
        /// scaled by the number of players and capped at <paramref name="maxEnemiesPerWave"/>.
        /// </summary>
        public static int EnemiesInWave(int waveIndex, int firstWaveEnemies, int players, int maxEnemiesPerWave)
        {
            if (waveIndex < 0) throw new ArgumentOutOfRangeException(nameof(waveIndex));
            if (players < 1) throw new ArgumentOutOfRangeException(nameof(players));

            int enemies = waveIndex == 0
                ? firstWaveEnemies * players
                : (int)Math.Round(firstWaveEnemies * waveIndex * 1.25 * players);

            return Math.Min(enemies, maxEnemiesPerWave);
        }

        /// <summary>Zombie health for a wave: linear growth on the base health.</summary>
        public static int ZombieHealth(int baseHealth, int waveIndex, float multiplierPerWave) =>
            (int)Math.Round(baseHealth * (waveIndex + 1) * multiplierPerWave);

        /// <summary>Zombie movement speed for a wave, never above <paramref name="maxSpeed"/>.</summary>
        public static float ZombieSpeed(float baseSpeed, int waveIndex, float multiplierPerWave, float maxSpeed) =>
            Math.Min(baseSpeed * (waveIndex + 1) * multiplierPerWave, maxSpeed);

        /// <summary>Index of the next wave, wrapping back to the first one after the last defined wave.</summary>
        public static int NextWaveIndex(int currentWaveIndex, int waveCount) =>
            waveCount <= 0 ? 0 : (currentWaveIndex + 1) % waveCount;

        /// <summary>
        /// Grenades the player owns after a wave refill: adds <paramref name="refill"/> without going over the limit.
        /// </summary>
        public static int RefillGrenades(int current, int refill, int max) =>
            Math.Max(current, Math.Min(current + Math.Max(0, refill), max));
    }
}
