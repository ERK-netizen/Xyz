using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class WorldBuilderTests
    {
        private static readonly SectorSpec Admin = new SectorSpec(SectorKind.Administrative, 3);
        private static readonly SectorSpec Res = new SectorSpec(SectorKind.Residential, 3);
        private static readonly SectorSpec Port = new SectorSpec(SectorKind.Port, 2);

        private static void AssertFails(WorldBuilder builder, string expectedKey)
        {
            Result<WorldMap> result = builder.Build();
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorKey, Does.StartWith(expectedKey));
        }

        [Test]
        public void CoastalMap_Builds_WithConsistentOwnershipAndCities()
        {
            WorldMap map = TestWorlds.Coastal(out var portCity, out var inlandCity);
            var violations = new List<string>();
            Assert.That(WorldInvariants.Check(map, violations), Is.EqualTo(0), string.Join("\n", violations));

            Assert.That(map.Regions.Count, Is.EqualTo(3));
            Assert.That(map.Cities.Count, Is.EqualTo(2));
            Assert.That(map.Sectors.Count, Is.EqualTo(7));

            HexCell portHex = map.Cell(new HexCoord(2, 3));
            Assert.That(portHex.IsCoastal, Is.True);
            Assert.That(map.Cell(new HexCoord(6, 3)).IsCoastal, Is.False);
            Assert.That(portHex.City, Is.EqualTo(portCity));
            Assert.That(portHex.Owner, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(portHex.Controller, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(map.Cell(new HexCoord(8, 4)).Owner, Is.EqualTo(TestWorlds.CountryB));
            Assert.That(map.Cell(new HexCoord(0, 0)).Owner.IsValid, Is.False, "bölgesiz okyanus sahipsiz");

            City inland = map.Cities[inlandCity];
            Assert.That(inland.Sectors.Count, Is.EqualTo(4));
            Assert.That(map.AdministrativeSectorOf(inland).Slot, Is.EqualTo(SectorSlot.Center));
            Assert.That(map.Sectors[inland.Sectors[1]].Slot, Is.EqualTo(SectorSlot.East));
            Assert.That(map.Sectors[inland.Sectors[1]].Kind, Is.EqualTo(SectorKind.Industrial));
            foreach (var region in new[] { map.Regions.ValueAt(0), map.Regions.ValueAt(1) })
            {
                Assert.That(region.Owner, Is.EqualTo(region.Controller), "başlangıçta sahip = kontrolcü");
            }
        }

        [Test]
        public void Rejects_CityOnWater() 
        {
            var b = TestWorlds.CoastalBuilder(out _, out _);
            b.AddCity("x", new HexCoord(0, 0), CityTier.Town, HexDirection.East, new[] { Admin, Res });
            AssertFails(b, WorldErrorKeys.SehirSudaOlamaz);
        }

        [Test]
        public void Rejects_SectorCountMismatch()
        {
            var b = TestWorlds.CoastalBuilder(out _, out _);
            b.AddCity("x", new HexCoord(4, 4), CityTier.City, HexDirection.East, new[] { Admin, Res });
            AssertFails(b, WorldErrorKeys.SehirSektorSayisi);
        }

        [Test]
        public void Rejects_MissingOrDuplicateAdministrative()
        {
            var b1 = TestWorlds.CoastalBuilder(out _, out _);
            b1.AddCity("x", new HexCoord(4, 4), CityTier.Town, HexDirection.East, new[] { Res, Admin });
            AssertFails(b1, WorldErrorKeys.SehirMerkezYok);

            var b2 = TestWorlds.CoastalBuilder(out _, out _);
            b2.AddCity("x", new HexCoord(4, 4), CityTier.Town, HexDirection.East, new[] { Admin, Admin });
            AssertFails(b2, WorldErrorKeys.SehirSektorTurleri);
        }

        [Test]
        public void Rejects_PortNotFacingWater()
        {
            var b = TestWorlds.CoastalBuilder(out _, out _);
            b.AddCity("x", new HexCoord(4, 4), CityTier.Town, HexDirection.East, new[] { Admin, Port });
            AssertFails(b, WorldErrorKeys.LimanKiyidaDegil);
        }

        [Test]
        public void Rejects_TwoCitiesOnOneHex()
        {
            var b = TestWorlds.CoastalBuilder(out _, out _);
            b.AddCity("x", new HexCoord(6, 3), CityTier.Town, HexDirection.East, new[] { Admin, Res });
            AssertFails(b, WorldErrorKeys.SehirHucresiDolu);
        }

        [Test]
        public void Rejects_RegionProblems()
        {
            var overlap = TestWorlds.CoastalBuilder(out _, out _);
            overlap.AddLandRegion("x", new[] { new HexCoord(3, 3) }, TestWorlds.CountryC);
            AssertFails(overlap, WorldErrorKeys.BolgeCakismasi);

            var mixed = TestWorlds.CoastalBuilder(out _, out _);
            mixed.AddLandRegion("x", new[] { new HexCoord(0, 0) }, TestWorlds.CountryC);
            AssertFails(mixed, WorldErrorKeys.BolgeKarisik);

            var empty = TestWorlds.CoastalBuilder(out _, out _);
            empty.AddLandRegion("x", new HexCoord[0], TestWorlds.CountryC);
            AssertFails(empty, WorldErrorKeys.BolgeBos);

            var duplicate = new WorldBuilder(TestWorlds.SmallGrid());
            duplicate.SetTerrain(new HexCoord(1, 1), TerrainType.Plains);
            duplicate.AddLandRegion("x", new[] { new HexCoord(1, 1), new HexCoord(1, 1) }, TestWorlds.CountryA);
            AssertFails(duplicate, WorldErrorKeys.BolgeCakismasi);
        }

        [Test]
        public void Rejects_InvalidInfrastructure()
        {
            var water = TestWorlds.CoastalBuilder(out _, out _);
            water.SetInfrastructure(new HexCoord(0, 0), new HexInfrastructure(1, 0, false));
            AssertFails(water, WorldErrorKeys.AltyapiSudaOlamaz);

            var port = TestWorlds.CoastalBuilder(out _, out _);
            port.SetInfrastructure(new HexCoord(6, 3), new HexInfrastructure(0, 0, true));
            AssertFails(port, WorldErrorKeys.LimanSehirGerektirir);

            var ok = TestWorlds.CoastalBuilder(out _, out _);
            ok.SetInfrastructure(new HexCoord(2, 3), new HexInfrastructure(2, 1, true));
            Assert.That(ok.Build().IsSuccess, Is.True);
        }

        [Test]
        public void Builder_IsSingleUse()
        {
            var b = TestWorlds.CoastalBuilder(out _, out _);
            Assert.That(b.Build().IsSuccess, Is.True);
            Assert.Throws<System.InvalidOperationException>(() => b.Build());
        }

        [Test]
        public void Invariants_DetectCorruption()
        {
            WorldMap map = TestWorlds.Coastal(out var portCity, out _);
            var violations = new List<string>();

            HexCell portHex = map.Cell(new HexCoord(2, 3));
            portHex.Controller = TestWorlds.CountryB; // merkez sektörle tutarsız
            Assert.That(WorldInvariants.Check(map, violations), Is.GreaterThan(0));
            Assert.That(violations, Has.Some.StartsWith(WorldErrorKeys.KontrolTutarsiz));

            violations.Clear();
            portHex.Controller = TestWorlds.CountryA;
            map.Sectors[map.Cities[portCity].Sectors[1]].BuildingSlotsUsed = 99;
            Assert.That(WorldInvariants.Check(map, violations), Is.EqualTo(1));
            Assert.That(violations[0], Does.StartWith(WorldErrorKeys.SektorSlotKapasitesi));
        }
    }
}
