using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Tests.Military
{
    [TestFixture]
    public class LandWarfareTests
    {
        private static readonly LandMovementCostModel Land = new LandMovementCostModel(FrontScenario.Content.Movement);

        private static void AssertInvariants(FrontScenario s)
        {
            var violations = new List<string>();
            Assert.That(WarfareInvariants.Check(s.War, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void Recruit_TakesRealManpowerAndEquipment_DisbandReturnsThem()
        {
            var s = new FrontScenario();
            HexCoord cityA = s.State.World.Grid.FromId(s.CityA.Hex);
            Assert.That(UnitRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.InfantryDivision, new HexCoord(104, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.ToplamaYeriGecersiz), "şehir değil");
            Assert.That(UnitRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.InfantryDivision, s.State.World.Grid.FromId(s.CityB.Hex)).ErrorKey, Is.EqualTo(WarfareErrorKeys.ToplamaYeriGecersiz), "düşman şehri");
            Assert.That(UnitRules.CheckRecruit(s.War, TestWorlds.CountryA, new UnitTypeId(9), cityA).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikTuruBilinmiyor));

            RegionPopulation west = s.State.Population.Regions[s.West];
            Fixed64 stockBefore = s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.InfantryEquipment);
            Result<LandUnitId> id = UnitRules.Recruit(s.War, TestWorlds.CountryA, BaselineUnits.InfantryDivision, cityA);
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            LandUnit unit = s.State.Warfare.Units[id.Value];
            Assert.That(west.Mobilized, Is.EqualTo(10_000));
            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.InfantryEquipment), Is.EqualTo(stockBefore - Fixed64.FromInt(100)));
            Assert.That(unit.Organization, Is.EqualTo(Fixed64.FromInt(30)), "yeni birlik %50 teşkilat");
            Assert.That(unit.HomeRegion, Is.EqualTo(s.West));

            s.State.Economy.Countries[TestWorlds.CountryA].RemoveStock(BaselineGoods.InfantryEquipment, Fixed64.FromInt(1_850));
            Assert.That(UnitRules.CheckRecruit(s.War, TestWorlds.CountryA, BaselineUnits.InfantryDivision, cityA).ErrorKey, Is.EqualTo(EconomyErrorKeys.StokYetersiz), "ekipman yoktan gelmez");

            Assert.That(UnitRules.Disband(s.War, TestWorlds.CountryA, id.Value).IsValid, Is.True);
            Assert.That(west.Mobilized, Is.EqualTo(0));
            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.InfantryEquipment), Is.EqualTo(Fixed64.FromInt(150)));
        }

        [Test]
        public void Movement_FollowsBudget_AndStopsAtEnemyUnitsAndNeutrals()
        {
            var s = new FrontScenario();
            LandUnit unit = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 102, 4);
            var path = new List<HexId>();
            Assert.That(MovementRules.CheckMove(s.War, new HexPathfinder(s.State.World, Land), TestWorlds.CountryA, unit.Id, new HexCoord(107, 4), path).IsValid, Is.True);
            Assert.That(path.Count, Is.EqualTo(5));
            MovementRules.SetPath(unit, path);
            var notes = new List<string>();
            MovementRules.AdvanceDay(s.War, Land, notes);
            Assert.That(unit.Hex, Is.EqualTo(s.State.World.Grid.ToId(new HexCoord(102, 4))), "≈43 km adım > 30 km/gün bütçe");
            MovementRules.AdvanceDay(s.War, Land, notes);
            Assert.That(unit.Hex, Is.EqualTo(s.State.World.Grid.ToId(new HexCoord(103, 4))));

            s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 105, 4);
            for (int d = 0; d < 6; d++) MovementRules.AdvanceDay(s.War, Land, notes);
            Assert.That(unit.Hex, Is.EqualTo(s.State.World.Grid.ToId(new HexCoord(104, 4))), "düşman birliğinin önünde durur");
            Assert.That(unit.Path.Count, Is.EqualTo(0));
            Assert.That(notes, Has.Some.StartsWith(MovementRules.BlockedNote));

            Assert.That(UnitQueries.CanEnter(s.War, TestWorlds.CountryA, s.Cell(120, 4)), Is.False, "tarafsız toprak (savaş yok)");
            Assert.That(UnitQueries.CanEnter(s.War, TestWorlds.CountryA, s.Cell(112, 4)), Is.True, "savaştığımız ülkenin şehirsiz toprağı");
            Assert.That(UnitQueries.CanEnter(s.War, TestWorlds.CountryA, s.Cell(114, 4)), Is.False, "düşman şehri yürüyerek alınmaz");
        }

        [Test]
        public void LostBattle_DefenderRetreats_AttackerAdvances_AndOccupies()
        {
            var s = new FrontScenario();
            LandUnit defender = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 110, 4);
            defender.Organization = Fixed64.FromInt(19); // kırılmanın eşiğinde
            var attackers = new List<LandUnit> { s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 109, 4), s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 109, 4) };
            var rng = new RngStream(1, RngSubsystem.Combat, 1, 1, 1);
            var notes = new List<string>();
            long eastMobilized = s.State.Population.Regions[s.East].Mobilized;
            CombatRules.Resolve(s.War, attackers, new HexCoord(110, 4), ref rng, notes);

            Assert.That(defender.Hex, Is.Not.EqualTo(s.State.World.Grid.ToId(new HexCoord(110, 4))), "geri çekildi");
            Assert.That(attackers[0].Hex, Is.EqualTo(s.State.World.Grid.ToId(new HexCoord(110, 4))), "saldırgan ilerledi");
            Assert.That(attackers[1].Hex, Is.EqualTo(attackers[0].Hex));
            Assert.That(s.State.Population.Regions[s.East].Mobilized, Is.LessThan(eastMobilized), "kayıplar nüfustan düşer");
            WarfareControlRules.Occupy(s.War);
            Assert.That(s.Cell(110, 4).Controller, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(s.Cell(110, 4).Owner, Is.EqualTo(TestWorlds.CountryB), "işgal sahipliği değiştirmez");
            AssertInvariants(s);
        }

        [Test]
        public void EncircledDefender_IsDestroyed()
        {
            var s = new FrontScenario();
            LandUnit defender = s.Place(TestWorlds.CountryB, BaselineUnits.InfantryDivision, 112, 6);
            defender.Organization = Fixed64.One;
            HexCoord here = new HexCoord(112, 6);
            for (int d = 0; d < 6; d++)
            {
                s.State.World.Grid.TryGetNeighbor(here, (HexDirection)d, out HexCoord n);
                s.State.World.SetHexController(n, TestWorlds.CountryA);
            }

            LandUnit attacker = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 111, 6);
            var rng = new RngStream(3, RngSubsystem.Combat, 1, 1, 1);
            var notes = new List<string>();
            long before = s.State.Population.Regions[s.East].Total;
            CombatRules.Resolve(s.War, new List<LandUnit> { attacker }, here, ref rng, notes);
            Assert.That(s.State.Warfare.Units.ContainsKey(defender.Id), Is.False);
            Assert.That(notes, Has.Some.StartsWith(CombatRules.UnitDestroyedNote));
            Assert.That(s.State.Population.Regions[s.East].Total, Is.LessThan(before - 9_000), "kuşatılan tümen kaybedilir");
        }

        [Test]
        public void EmptyEnemyCity_FallsSectorBySector_AdministrativeLast()
        {
            var s = new FrontScenario();
            City city = s.CityB;
            Assert.That(CombatRules.FortressFactor(s.War, s.Cell(114, 4), TestWorlds.CountryB), Is.EqualTo(Fixed64.Parse("1.25")));
            var force = new List<LandUnit>();
            for (int i = 0; i < 3; i++) force.Add(s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 113, 4));
            var captured = new List<SectorSlot>();
            var notes = new List<string>();
            for (int day = 0; day < 40 && s.Cell(114, 4).Controller != TestWorlds.CountryA; day++)
            {
                foreach (LandUnit u in force) u.Organization = Fixed64.FromInt(60);
                var rng = new RngStream(7, RngSubsystem.Combat, day, 1, 1);
                CombatRules.Resolve(s.War, force, new HexCoord(114, 4), ref rng, notes);
                foreach (SectorId id in city.Sectors)
                {
                    CitySector sector = s.State.World.Sectors[id];
                    if (sector.Controller == TestWorlds.CountryA && !captured.Contains(sector.Slot)) captured.Add(sector.Slot);
                }
            }

            Assert.That(captured.Count, Is.EqualTo(4));
            Assert.That(captured[0], Is.EqualTo(SectorSlot.West), "batıdan gelen saldırı batıya bakan kale yuvasından girer");
            Assert.That(captured[3], Is.EqualTo(SectorSlot.Center), "merkez en son");
            Assert.That(s.Cell(114, 4).Controller, Is.EqualTo(TestWorlds.CountryA));
            Assert.That(s.Cell(114, 4).Owner, Is.EqualTo(TestWorlds.CountryB));
            Assert.That(force[0].Hex, Is.EqualTo(city.Hex), "merkez düşünce birlikler şehre girer");
            AssertInvariants(s);
        }

        [Test]
        public void Siege_IsDetected_AndCutsTheCityAsSupplySource()
        {
            var s = new FrontScenario();
            HexCoord cityHex = s.State.World.Grid.FromId(s.CityB.Hex);
            for (int d = 0; d < 6; d++)
            {
                s.State.World.Grid.TryGetNeighbor(cityHex, (HexDirection)d, out HexCoord n);
                s.State.World.SetHexController(n, TestWorlds.CountryA);
            }

            WarfareControlRules.UpdateSieges(s.War);
            Assert.That(s.State.Warfare.IsBesieged(s.CityB.Id), Is.True);
            Assert.That(s.State.Warfare.IsBesieged(s.CityA.Id), Is.False);
            s.State.World.SetHexController(new HexCoord(113, 4), TestWorlds.CountryB);
            WarfareControlRules.UpdateSieges(s.War);
            Assert.That(s.State.Warfare.IsBesieged(s.CityB.Id), Is.False, "bir koridor kuşatmayı kırar");
        }

        [Test]
        public void OutOfSupply_Bleeds_SuppliedRecoversAndReinforcesFromStock()
        {
            var s = new FrontScenario();
            LandUnit cut = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 112, 7);
            LandUnit home = s.Place(TestWorlds.CountryA, BaselineUnits.InfantryDivision, 103, 4);
            home.Organization = Fixed64.FromInt(20);
            home.Manpower = 9_000;
            long mobilizedBefore = s.State.Population.Regions[s.West].Mobilized;
            Fixed64 stock = s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.InfantryEquipment);
            home.Equipment = Fixed64.Parse("0.9");

            var grid = SupplyRules.Compute(s.State.World, Land, OwnTerritorySupplyAccess.Instance, TestWorlds.CountryA,
                new[] { s.State.World.Grid.FromId(s.CityA.Hex) }, FrontScenario.Content.Logistics.BaseSupplyRange);
            var grids = new TarihStrateji.Core.Domain.Collections.StableIdMap<CountryId, SupplyGrid>();
            grids.Add(TestWorlds.CountryA, grid);
            SustainmentRules.ApplyDay(s.War, grids, new List<string>());

            Assert.That(cut.Supply, Is.EqualTo(Fixed64.Zero));
            Assert.That(cut.Organization, Is.EqualTo(Fixed64.FromInt(57)), "−%5 teşkilat");
            Assert.That(cut.Manpower, Is.EqualTo(9_950), "−%0,5 güç");
            Assert.That(home.Supply.IsPositive, Is.True);
            Assert.That(home.Organization > Fixed64.FromInt(20), Is.True);
            Assert.That(home.Manpower, Is.EqualTo(9_200), "takviye: tam gücün %2'si");
            Assert.That(home.Equipment, Is.EqualTo(Fixed64.Parse("0.92")));
            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.InfantryEquipment), Is.EqualTo(stock - Fixed64.FromInt(2)), "ekipman envanterden");
            Assert.That(s.State.Population.Regions[s.West].Mobilized, Is.EqualTo(mobilizedBefore + 200 - 50), "takviye seferber, kayıp düşüldü");
        }

        [Test]
        public void Snapshot_RoundTrips()
        {
            var s = new FrontScenario();
            LandUnit u = s.Place(TestWorlds.CountryA, BaselineUnits.CavalryDivision, 104, 4);
            MovementRules.SetPath(u, new[] { s.State.World.Grid.ToId(new HexCoord(105, 4)) });
            s.Place(TestWorlds.CountryB, BaselineUnits.ArtilleryRegiment, 115, 4);
            WarfareControlRules.UpdateSieges(s.War);
            var w = new CanonicalWriter();
            WarfareSnapshotCodec.Write(s.State.Warfare, w);
            Result<WarfareState> back = WarfareSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            WarfareSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
        }
    }
}
