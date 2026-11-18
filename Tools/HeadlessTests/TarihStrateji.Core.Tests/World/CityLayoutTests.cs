using NUnit.Framework;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class CityLayoutTests
    {
        [Test]
        public void EveryTierAndFacing_ProducesConnectedLayout_WithCenterFirst()
        {
            foreach (CityTier tier in new[] { CityTier.Town, CityTier.SmallCity, CityTier.City, CityTier.LargeCity, CityTier.Metropolis })
            {
                for (int d = 0; d < 6; d++)
                {
                    SectorSlot[] slots = CityLayout.SlotsFor(tier, (HexDirection)d);
                    Assert.That(slots.Length, Is.EqualTo((int)tier));
                    Assert.That(slots[0], Is.EqualTo(SectorSlot.Center));
                    Assert.That(slots[1], Is.EqualTo(CityLayout.RingSlot((HexDirection)d)));
                    Assert.That(CityLayout.IsConnected(slots), Is.True);
                }
            }

            Assert.Throws<System.ArgumentOutOfRangeException>(() => CityLayout.SlotsFor((CityTier)7, HexDirection.East));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => CityLayout.SlotsFor((CityTier)1, HexDirection.East));
        }

        [Test]
        public void Adjacency_And_Connectivity()
        {
            Assert.That(CityLayout.AreAdjacent(SectorSlot.East, SectorSlot.SouthEast), Is.True);
            Assert.That(CityLayout.AreAdjacent(SectorSlot.East, SectorSlot.NorthEast), Is.True);
            Assert.That(CityLayout.AreAdjacent(SectorSlot.East, SectorSlot.West), Is.False);
            Assert.That(CityLayout.AreAdjacent(SectorSlot.Center, SectorSlot.West), Is.True);
            Assert.That(CityLayout.AreAdjacent(SectorSlot.West, SectorSlot.West), Is.False);
            Assert.That(CityLayout.IsConnected(new[] { SectorSlot.East, SectorSlot.West }), Is.False);
            Assert.That(CityLayout.IsConnected(new[] { SectorSlot.East, SectorSlot.NorthEast }), Is.True);
            Assert.That(CityLayout.IsConnected(new[] { SectorSlot.Center, SectorSlot.Center }), Is.False);
            Assert.That(CityLayout.IsConnected(new SectorSlot[0]), Is.False);
        }

        [Test]
        public void EntrySlot_FacesTheAttacker_OrNearestRing_OrCenter()
        {
            SectorSlot[] slots = CityLayout.SlotsFor(CityTier.SmallCity, HexDirection.East); // C, E, NE
            Assert.That(CityLayout.EntrySlotFrom(HexDirection.East, slots), Is.EqualTo(SectorSlot.East));
            Assert.That(CityLayout.EntrySlotFrom(HexDirection.NorthWest, slots), Is.EqualTo(SectorSlot.NorthEast));
            Assert.That(CityLayout.EntrySlotFrom(HexDirection.SouthEast, slots), Is.EqualTo(SectorSlot.East));
            Assert.That(CityLayout.EntrySlotFrom(HexDirection.West, slots), Is.EqualTo(SectorSlot.NorthEast));
            Assert.That(CityLayout.EntrySlotFrom(HexDirection.West, new[] { SectorSlot.Center }), Is.EqualTo(SectorSlot.Center));
        }
    }
}
