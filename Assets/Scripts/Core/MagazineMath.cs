using System;

namespace ZBattleYard.Core
{
    /// <summary>Ammo state of one weapon slot: rounds in the magazine and rounds in reserve.</summary>
    public readonly struct AmmoState : IEquatable<AmmoState>
    {
        public readonly int Magazine;
        public readonly int Reserve;

        public AmmoState(int magazine, int reserve)
        {
            Magazine = magazine;
            Reserve = reserve;
        }

        public bool Equals(AmmoState other) => Magazine == other.Magazine && Reserve == other.Reserve;
        public override bool Equals(object obj) => obj is AmmoState other && Equals(other);
        public override int GetHashCode() => (Magazine * 397) ^ Reserve;
        public override string ToString() => $"{Magazine}/{Reserve}";
    }

    /// <summary>Pure reload and ammo rules, kept free of Unity types so they can be unit tested.</summary>
    public static class MagazineMath
    {
        /// <summary>
        /// Moves as many rounds as possible from the reserve into the magazine.
        /// The magazine never goes above <paramref name="magazineSize"/> and the reserve never goes below zero.
        /// </summary>
        public static AmmoState Reload(AmmoState current, int magazineSize)
        {
            if (magazineSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(magazineSize), "Magazine size must be positive.");

            int missing = Math.Max(0, magazineSize - current.Magazine);
            int loaded = Math.Min(missing, Math.Max(0, current.Reserve));
            return new AmmoState(current.Magazine + loaded, current.Reserve - loaded);
        }

        /// <summary>True when a reload would load at least one round.</summary>
        public static bool CanReload(AmmoState current, int magazineSize) =>
            current.Magazine < magazineSize && current.Reserve > 0;

        /// <summary>Removes one round from the magazine. Returns the same state if the magazine is empty.</summary>
        public static AmmoState Fire(AmmoState current) =>
            current.Magazine > 0 ? new AmmoState(current.Magazine - 1, current.Reserve) : current;

        /// <summary>True when both the magazine and the reserve are full.</summary>
        public static bool IsFull(AmmoState current, int magazineSize, int maxReserve) =>
            current.Magazine >= magazineSize && current.Reserve >= maxReserve;
    }
}
