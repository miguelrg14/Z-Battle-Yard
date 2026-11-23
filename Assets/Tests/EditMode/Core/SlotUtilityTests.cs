using NUnit.Framework;
using ZBattleYard.Core;

namespace ZBattleYard.Tests.Core
{
    public class SlotUtilityTests
    {
        class Item { }

        readonly Item pistol = new Item();
        readonly Item rifle = new Item();
        readonly Item shotgun = new Item();

        [Test]
        public void FirstEmptySlot_ReturnsFirstNullOrMinusOneWhenFull()
        {
            Assert.AreEqual(1, SlotUtility.FirstEmptySlot(new[] { pistol, null, null }));
            Assert.AreEqual(-1, SlotUtility.FirstEmptySlot(new[] { pistol, rifle }));
            Assert.AreEqual(-1, SlotUtility.FirstEmptySlot(new Item[0]));
        }

        [Test]
        public void IndexOf_ReturnsMinusOneForMissingItems()
        {
            var slots = new[] { pistol, rifle, null };

            Assert.AreEqual(1, SlotUtility.IndexOf(slots, rifle));
            Assert.AreEqual(-1, SlotUtility.IndexOf(slots, shotgun), "A missing weapon must not be reported as slot 0");
            Assert.AreEqual(-1, SlotUtility.IndexOf(slots, null));
        }

        [Test]
        public void NextOccupiedSlot_SkipsEmptySlotsAndWraps()
        {
            var slots = new[] { pistol, null, shotgun };

            Assert.AreEqual(2, SlotUtility.NextOccupiedSlot(slots, 0, +1));
            Assert.AreEqual(0, SlotUtility.NextOccupiedSlot(slots, 2, +1));
            Assert.AreEqual(2, SlotUtility.NextOccupiedSlot(slots, 0, -1));
        }

        [Test]
        public void NextOccupiedSlot_StaysWhenNoOtherWeapon()
        {
            Assert.AreEqual(0, SlotUtility.NextOccupiedSlot(new[] { pistol, null, null }, 0, +1));
        }
    }
}
