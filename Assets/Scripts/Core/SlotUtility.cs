using System.Collections.Generic;

namespace ZBattleYard.Core
{
    /// <summary>Helpers for fixed-size inventories (weapon slots, perk slots).</summary>
    public static class SlotUtility
    {
        /// <summary>Index of the first empty (null) slot, or -1 if every slot is used.</summary>
        public static int FirstEmptySlot<T>(IReadOnlyList<T> slots) where T : class
        {
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] == null)
                    return i;
            return -1;
        }

        /// <summary>Index of <paramref name="item"/> in <paramref name="slots"/>, or -1 if it isn't there.</summary>
        public static int IndexOf<T>(IReadOnlyList<T> slots, T item) where T : class
        {
            if (item == null) return -1;
            for (int i = 0; i < slots.Count; i++)
                if (ReferenceEquals(slots[i], item))
                    return i;
            return -1;
        }

        /// <summary>
        /// Next occupied slot when cycling with the mouse wheel or a gamepad button.
        /// Returns <paramref name="current"/> if no other slot holds an item.
        /// </summary>
        public static int NextOccupiedSlot<T>(IReadOnlyList<T> slots, int current, int direction) where T : class
        {
            int count = slots.Count;
            if (count == 0) return current;
            int step = direction >= 0 ? 1 : -1;

            for (int offset = 1; offset < count; offset++)
            {
                int index = ((current + step * offset) % count + count) % count;
                if (slots[index] != null)
                    return index;
            }
            return current;
        }
    }
}
