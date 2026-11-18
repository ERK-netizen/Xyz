using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Tests.World
{
    [TestFixture]
    public class ControlRulesTests
    {
        private static void AssertInvariants(WorldMap map)
        {
            var violations = new List<string>();
            Assert.That(WorldInvariants.Check(map, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void CapturingAdministrativeSector_ChangesControl_NotOwnership()
        {
            WorldMap map = TestWorlds.Coastal(out _, out CityId inland);
            City city = map.Cities[inland];
            HexCell hex = map.Cell(city.Hex);

            // İç şehir (6,3) doğu bölgesinde: sahibi B. Saldıran A; önce yan sektör düşer.
            Assert.That(hex.Owner, Is.EqualTo(TestWorlds.CountryB));
            Assert.That(map.SetSectorController(city.Sectors[1], TestWorlds.CountryA).IsValid, Is.True);
            Assert.That(hex.Controller, Is.EqualTo(TestWorlds.CountryB), "yan sektör şehri düşürmez");

            Assert.That(map.SetSectorController(city.AdministrativeSector, TestWorlds.CountryA).IsValid, Is.True);
            Assert.That(hex.Controller, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(hex.Owner, Is.EqualTo(TestWorlds.CountryB), "işgal sahipliği değiştirmez");
            Assert.That(map.Sectors[city.AdministrativeSector].Owner, Is.EqualTo(TestWorlds.CountryB));
            AssertInvariants(map);
        }

        [Test]
        public void HexWithCity_CannotBeControlledDirectly()
        {
            WorldMap map = TestWorlds.Coastal(out _, out CityId inland);
            HexCoord cityHex = map.Grid.FromId(map.Cities[inland].Hex);
            Assert.That(map.SetHexController(cityHex, TestWorlds.CountryB).ErrorKey, Is.EqualTo(WorldErrorKeys.KontrolTutarsiz));
            Assert.That(map.SetHexController(new HexCoord(99, 0), TestWorlds.CountryB).IsValid, Is.False);
            Assert.That(map.SetSectorController(new SectorId(999), TestWorlds.CountryB).ErrorKey, Is.EqualTo(WorldErrorKeys.SektorBulunamadi));
        }

        [Test]
        public void RegionController_FollowsMajority_WithStableTieBreak()
        {
            WorldMap map = TestWorlds.Coastal(out _, out _);
            StrategicRegion east = map.Regions.ValueAt(1); // 4×4 = 16 hücre, B; biri şehir hücresi
            Assert.That(east.Hexes.Count, Is.EqualTo(16));
            var open = new List<HexCoord>();
            foreach (HexId id in east.Hexes)
            {
                if (!map.Cell(id).City.IsValid)
                {
                    open.Add(map.Grid.FromId(id));
                }
            }

            Assert.That(open.Count, Is.EqualTo(15));
            for (int i = 0; i < 8; i++)
            {
                Assert.That(map.SetHexController(open[i], TestWorlds.CountryC).IsValid, Is.True);
            }

            Assert.That(east.Controller, Is.EqualTo(TestWorlds.CountryB), "C 8 – B 8 eşitliğinde mevcut kontrolcü korunur");

            map.SetHexController(open[8], TestWorlds.CountryC);
            Assert.That(east.Controller, Is.EqualTo(TestWorlds.CountryC), "C 9 – B 7 çoğunluk");
            Assert.That(east.Owner, Is.EqualTo(TestWorlds.CountryB));

            // C 7, B 7, A 2: mevcut kontrolcü (C) eşitler arasında → korunur.
            map.SetHexController(open[0], TestWorlds.CountryA);
            map.SetHexController(open[1], TestWorlds.CountryA);
            Assert.That(east.Controller, Is.EqualTo(TestWorlds.CountryC));

            // C 6, B 8, A 2: B tek çoğunluk.
            map.SetHexController(open[2], TestWorlds.CountryB);
            Assert.That(east.Controller, Is.EqualTo(TestWorlds.CountryB));

            // C 5, B 8, A 3: B çoğunlukta kalır.
            map.SetHexController(open[3], TestWorlds.CountryA);
            Assert.That(east.Controller, Is.EqualTo(TestWorlds.CountryB));

            // Kontrolcüsüz çoğunluk: C'nin 5 hücresi + 4 B hücresi kontrolsüz → None 9, B 4, A 3.
            for (int i = 4; i < 13; i++)
            {
                map.SetHexController(open[i], CountryId.None);
            }

            Assert.That(east.Controller.IsValid, Is.False, "kontrolsüz hücreler çoğunluktaysa bölge kontrolsüz");
            AssertInvariants(map);
        }

        [Test]
        public void TransferOwnership_MovesOwnerEverywhere_KeepsControllers()
        {
            WorldMap map = TestWorlds.Coastal(out CityId portCity, out _);
            StrategicRegion west = map.Regions.ValueAt(0);
            City port = map.Cities[portCity];
            map.SetSectorController(port.AdministrativeSector, TestWorlds.CountryB);

            Assert.That(map.TransferRegionOwnership(west.Id, TestWorlds.CountryB).IsValid, Is.True);
            Assert.That(west.Owner, Is.EqualTo(TestWorlds.CountryB));
            foreach (HexId id in west.Hexes)
            {
                Assert.That(map.Cell(id).Owner, Is.EqualTo(TestWorlds.CountryB));
            }

            foreach (SectorId s in port.Sectors)
            {
                Assert.That(map.Sectors[s].Owner, Is.EqualTo(TestWorlds.CountryB));
            }

            Assert.That(map.Sectors[port.Sectors[1]].Controller, Is.EqualTo(TestWorlds.CountryA), "kontrol devirle değişmez");
            Assert.That(map.TransferRegionOwnership(map.Regions.ValueAt(2).Id, TestWorlds.CountryA).IsValid, Is.False, "deniz devredilemez");
            Assert.That(map.TransferRegionOwnership(new RegionId(42), TestWorlds.CountryA).ErrorKey, Is.EqualTo(WorldErrorKeys.BolgeBulunamadi));
            AssertInvariants(map);
        }

        [Test]
        public void Infrastructure_DoesNotConsumeBuildingSlots()
        {
            WorldMap map = TestWorlds.Coastal(out CityId portCity, out _);
            City port = map.Cities[portCity];
            HexCoord hex = map.Grid.FromId(port.Hex);
            int before = 0;
            foreach (SectorId s in port.Sectors)
            {
                before += map.Sectors[s].FreeBuildingSlots;
            }

            Assert.That(map.SetInfrastructure(hex, new HexInfrastructure(HexInfrastructure.MaxRoadLevel, HexInfrastructure.MaxRailLevel, true)).IsValid, Is.True);
            int after = 0;
            foreach (SectorId s in port.Sectors)
            {
                after += map.Sectors[s].FreeBuildingSlots;
            }

            Assert.That(after, Is.EqualTo(before));
            Assert.That(map.Cell(hex).Infrastructure.HasPort, Is.True);
            Assert.That(map.SetInfrastructure(new HexCoord(0, 0), new HexInfrastructure(0, 1, false)).ErrorKey, Is.EqualTo(WorldErrorKeys.AltyapiSudaOlamaz));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new HexInfrastructure(4, 0, false));
        }

        [Test]
        public void BuildingSlots_AreBounded()
        {
            WorldMap map = TestWorlds.Coastal(out CityId portCity, out _);
            SectorId admin = map.Cities[portCity].AdministrativeSector; // kapasite 4
            Assert.That(map.UseBuildingSlots(admin, 3).IsValid, Is.True);
            Assert.That(map.UseBuildingSlots(admin, 2).ErrorKey, Is.EqualTo(WorldErrorKeys.SektorSlotKapasitesi));
            Assert.That(map.UseBuildingSlots(admin, 0).IsValid, Is.False);
            Assert.That(map.ReleaseBuildingSlots(admin, 4).IsValid, Is.False);
            Assert.That(map.ReleaseBuildingSlots(admin, 3).IsValid, Is.True);
            Assert.That(map.Sectors[admin].FreeBuildingSlots, Is.EqualTo(4));
            AssertInvariants(map);
        }
    }
}
