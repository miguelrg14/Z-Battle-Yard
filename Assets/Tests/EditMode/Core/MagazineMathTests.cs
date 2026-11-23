using System;
using NUnit.Framework;
using ZBattleYard.Core;

namespace ZBattleYard.Tests.Core
{
    public class MagazineMathTests
    {
        [Test]
        public void Reload_FillsMagazineFromReserve()
        {
            AmmoState result = MagazineMath.Reload(new AmmoState(3, 90), magazineSize: 30);

            Assert.AreEqual(new AmmoState(30, 63), result);
        }

        [Test]
        public void Reload_WithSmallReserve_LoadsOnlyWhatIsLeft()
        {
            AmmoState result = MagazineMath.Reload(new AmmoState(5, 4), magazineSize: 30);

            Assert.AreEqual(new AmmoState(9, 0), result);
        }

        [Test]
        public void Reload_WithEmptyReserve_DoesNothing()
        {
            AmmoState result = MagazineMath.Reload(new AmmoState(12, 0), magazineSize: 30);

            Assert.AreEqual(new AmmoState(12, 0), result);
        }

        [Test]
        public void Reload_WithFullMagazine_KeepsReserve()
        {
            AmmoState result = MagazineMath.Reload(new AmmoState(8, 40), magazineSize: 8);

            Assert.AreEqual(new AmmoState(8, 40), result);
        }

        [TestCase(0, 1, 8, 1, 0)]
        [TestCase(7, 100, 8, 8, 99)]
        [TestCase(0, 0, 8, 0, 0)]
        [TestCase(2, 3, 8, 5, 0)]
        public void Reload_NeverExceedsMagazineOrGoesNegative(int magazine, int reserve, int size, int expectedMagazine, int expectedReserve)
        {
            AmmoState result = MagazineMath.Reload(new AmmoState(magazine, reserve), size);

            Assert.AreEqual(expectedMagazine, result.Magazine);
            Assert.AreEqual(expectedReserve, result.Reserve);
            Assert.That(result.Magazine + result.Reserve, Is.EqualTo(magazine + reserve), "Ammo must be conserved");
        }

        [Test]
        public void Reload_WithInvalidMagazineSize_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MagazineMath.Reload(new AmmoState(0, 10), 0));
        }

        [TestCase(5, 10, 8, true)]
        [TestCase(8, 10, 8, false)]
        [TestCase(5, 0, 8, false)]
        public void CanReload_OnlyWhenMagazineIsMissingRoundsAndReserveHasSome(int magazine, int reserve, int size, bool expected)
        {
            Assert.AreEqual(expected, MagazineMath.CanReload(new AmmoState(magazine, reserve), size));
        }

        [Test]
        public void Fire_UsesOneRound_AndNeverGoesBelowZero()
        {
            Assert.AreEqual(new AmmoState(4, 10), MagazineMath.Fire(new AmmoState(5, 10)));
            Assert.AreEqual(new AmmoState(0, 10), MagazineMath.Fire(new AmmoState(0, 10)));
        }

        [Test]
        public void IsFull_RequiresFullMagazineAndReserve()
        {
            Assert.IsTrue(MagazineMath.IsFull(new AmmoState(30, 120), 30, 120));
            Assert.IsFalse(MagazineMath.IsFull(new AmmoState(29, 120), 30, 120));
            Assert.IsFalse(MagazineMath.IsFull(new AmmoState(30, 119), 30, 120));
        }
    }
}
