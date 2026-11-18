using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Persistence;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class WorldSnapshotTests
    {
        private static byte[] Encode(WorldMap map)
        {
            var w = new CanonicalWriter();
            WorldSnapshotCodec.Write(map, w);
            return w.ToArray();
        }

        [Test]
        public void RoundTrip_PreservesEverything_ByteForByte()
        {
            WorldMap map = TestWorlds.Coastal(out var port, out var inland);
            map.SetInfrastructure(new HexCoord(2, 3), new HexInfrastructure(3, 2, true));
            map.SetSectorController(map.Cities[inland].AdministrativeSector, TestWorlds.CountryA);
            map.UseBuildingSlots(map.Cities[port].Sectors[2], 2);
            map.TransferRegionOwnership(map.Regions.ValueAt(1).Id, TestWorlds.CountryC);
            byte[] first = Encode(map);

            Result<WorldMap> restored = WorldSnapshotCodec.Read(new CanonicalReader(first));
            Assert.That(restored.IsSuccess, Is.True, restored.ErrorKey);
            Assert.That(Encode(restored.Value), Is.EqualTo(first));
            WorldMap copy = restored.Value;
            Assert.That(copy.Cell(new HexCoord(2, 3)).IsCoastal, Is.True, "türetilen kıyı bilgisi yeniden hesaplanır");
            Assert.That(copy.Cell(new HexCoord(6, 3)).Controller, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(copy.Cell(new HexCoord(6, 3)).Owner, Is.EqualTo(TestWorlds.CountryC));
            Assert.That(copy.Sectors[copy.Cities[port].Sectors[2]].BuildingSlotsUsed, Is.EqualTo(2));
            Assert.That(copy.Distances.MinimumNeighborKm, Is.EqualTo(map.Distances.MinimumNeighborKm));
            Assert.That(copy.CityIds.NextValue, Is.EqualTo(map.CityIds.NextValue));
        }

        [Test]
        public void InconsistentSnapshot_IsRejectedByInvariants()
        {
            WorldMap map = TestWorlds.Coastal(out var port, out _);
            map.Sectors[map.Cities[port].AdministrativeSector].Controller = TestWorlds.CountryC; // hücreyle tutarsız
            Result<WorldMap> result = WorldSnapshotCodec.Read(new CanonicalReader(Encode(map)));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorKey, Does.StartWith(DomainErrorKeys.KayitBozuk));
        }

        [Test]
        public void GarbageAndTruncation_AreRejected()
        {
            byte[] good = Encode(TestWorlds.Coastal(out _, out _));
            Assert.That(() => WorldSnapshotCodec.Read(new CanonicalReader(good, 0, good.Length - 5)), Throws.TypeOf<System.FormatException>());
            byte[] badTerrain = (byte[])good.Clone();
            badTerrain[4 + 4 + 4 + 8 + 8 + 24 + 4] = 200; // ilk hücrenin arazi baytı
            Assert.That(WorldSnapshotCodec.Read(new CanonicalReader(badTerrain)).ErrorKey, Is.EqualTo(DomainErrorKeys.KayitBozuk));
        }
    }
}
