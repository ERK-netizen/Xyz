using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Tests.World;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Tests.Naval
{
    [TestFixture]
    public class NavalRulesTests
    {
        private static void AssertInvariants(StraitScenario s)
        {
            var violations = new List<string>();
            Assert.That(WarfareInvariants.Check(s.War, violations), Is.EqualTo(0), string.Join("\n", violations));
        }

        [Test]
        public void Fleet_IsBuiltOnlyInAHomePort_FromRealShipsAndCrew()
        {
            var s = new StraitScenario();
            WarfareContext war = s.War;
            Assert.That(FleetRules.CheckBuild(war, TestWorlds.CountryA, BaselineUnits.BattleshipSquadron, StraitScenario.At(s.InlandA, s.State.World)).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.LimanGecersiz), "limansız şehir");
            Assert.That(FleetRules.CheckBuild(war, TestWorlds.CountryA, BaselineUnits.BattleshipSquadron, StraitScenario.At(s.PortB, s.State.World)).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.LimanGecersiz), "düşman limanı");
            Assert.That(FleetRules.CheckBuild(war, TestWorlds.CountryA, BaselineUnits.InfantryDivision, StraitScenario.At(s.PortA, s.State.World)).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.FiloTuruBilinmiyor));

            Result<FleetId> id = FleetRules.Build(war, TestWorlds.CountryA, BaselineUnits.BattleshipSquadron, StraitScenario.At(s.PortA, s.State.World));
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.Ship), Is.EqualTo(Fixed64.FromInt(36)));
            Assert.That(s.State.Population.Regions[s.West].Mobilized, Is.EqualTo(3_000));

            s.State.Economy.Countries[TestWorlds.CountryA].RemoveStock(BaselineGoods.Ship, Fixed64.FromInt(35));
            Assert.That(FleetRules.CheckBuild(war, TestWorlds.CountryA, BaselineUnits.BattleshipSquadron, StraitScenario.At(s.PortA, s.State.World)).IsValid,
                Is.False, "gemiler yoktan gelmez");

            Assert.That(FleetRules.Disband(war, TestWorlds.CountryA, id.Value).IsValid, Is.True);
            Assert.That(s.State.Economy.Countries[TestWorlds.CountryA].Stock(BaselineGoods.Ship), Is.EqualTo(Fixed64.FromInt(5)));
            Assert.That(s.State.Population.Regions[s.West].Mobilized, Is.EqualTo(0));
        }

        [Test]
        public void Embark_RequiresTransportCapacity_AndRespectsIt()
        {
            var s = new StraitScenario();
            HexCoord port = StraitScenario.At(s.PortA, s.State.World);
            Fleet destroyers = s.Fleet(TestWorlds.CountryA, BaselineUnits.DestroyerFlotilla, port.Column, port.Row);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
            var units = new[] { s.Unit(TestWorlds.CountryA, 109, 4), s.Unit(TestWorlds.CountryA, 109, 4), s.Unit(TestWorlds.CountryA, 109, 4) };
            LandUnit away = s.Unit(TestWorlds.CountryA, 105, 4);

            Assert.That(AmphibiousRules.CheckEmbark(s.War, TestWorlds.CountryA, units[0].Id, destroyers.Id).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.NakliyeKapasitesiYok), "transportCapacity == 0 → reddedilir");
            Assert.That(AmphibiousRules.CheckEmbark(s.War, TestWorlds.CountryA, away.Id, transports.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.AyniLimandaDegil));

            AmphibiousRules.Embark(s.War, units[0], transports);
            AmphibiousRules.Embark(s.War, units[1], transports);
            Assert.That(AmphibiousRules.CheckEmbark(s.War, TestWorlds.CountryA, units[2].Id, transports.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.NakliyeKapasitesiDolu));
            Assert.That(AmphibiousRules.CheckEmbark(s.War, TestWorlds.CountryA, units[0].Id, transports.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikGemide));

            Assert.That(MovementRules.CheckMove(s.War, new HexPathfinder(s.State.World, new LandMovementCostModel(StraitScenario.Content.Movement)), TestWorlds.CountryA, units[0].Id, new HexCoord(105, 4), new List<HexId>()).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.BirlikGemide));
            Assert.That(CombatRules.CheckAttack(s.War, TestWorlds.CountryA, units[0].Id, new HexCoord(108, 4)).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikGemide));
            Assert.That(UnitRules.CheckDisband(s.War, TestWorlds.CountryA, units[0].Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikGemide));
            Assert.That(FleetRules.CheckDisband(s.War, TestWorlds.CountryA, transports.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.FiloYuklu));
            AssertInvariants(s);
        }

        [Test]
        public void LoadedFleet_CrossesTheStrait_AndLandingRulesHold()
        {
            var s = new StraitScenario();
            HexCoord port = StraitScenario.At(s.PortA, s.State.World);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
            LandUnit unit = s.Unit(TestWorlds.CountryA, 109, 4);
            Assert.That(AmphibiousRules.CheckLanding(s.War, TestWorlds.CountryA, unit.Id, new HexCoord(113, 3)).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikGemideDegil));
            AmphibiousRules.Embark(s.War, unit, transports);

            var path = new List<HexId>();
            var naval = new HexPathfinder(s.State.World, new NavalMovementCostModel(s.State.World));
            Assert.That(FleetRules.CheckMove(s.War, naval, TestWorlds.CountryA, transports.Id, new HexCoord(105, 4), path).ErrorKey, Is.EqualTo(WarfareErrorKeys.HedefGecersiz), "kara hücresi");
            Assert.That(FleetRules.CheckMove(s.War, naval, TestWorlds.CountryA, transports.Id, new HexCoord(112, 3), path).IsValid, Is.True);
            FleetRules.SetPath(transports, path);
            for (int d = 0; d < 3; d++)
            {
                FleetRules.AdvanceDay(s.War, new NavalMovementCostModel(s.State.World), new List<string>());
            }

            Assert.That(transports.Hex, Is.EqualTo(s.Hex(112, 3)));
            Assert.That(unit.Hex, Is.EqualTo(transports.Hex), "taşınan birlik filoyla gider");
            AssertInvariants(s);

            Assert.That(AmphibiousRules.CheckLanding(s.War, TestWorlds.CountryA, unit.Id, new HexCoord(113, 3)).IsValid, Is.True, "boş düşman kıyısı");
            Assert.That(AmphibiousRules.CheckLanding(s.War, TestWorlds.CountryA, unit.Id, new HexCoord(115, 3)).ErrorKey, Is.EqualTo(WarfareErrorKeys.HedefKomsuDegil));
            Assert.That(AmphibiousRules.CheckLanding(s.War, TestWorlds.CountryA, unit.Id, new HexCoord(111, 3)).ErrorKey, Is.EqualTo(WarfareErrorKeys.CikarmaHedefiGecersiz), "deniz");
            Assert.That(AmphibiousRules.CheckLanding(s.War, TestWorlds.CountryA, unit.Id, StraitScenario.At(s.PortB, s.State.World)).IsValid, Is.True, "düşman limanına çıkarma muharebesi");
            Assert.That(AmphibiousRules.IsAssault(s.War, TestWorlds.CountryA, s.Cell(113, 4)), Is.True);
            Assert.That(AmphibiousRules.IsAssault(s.War, TestWorlds.CountryA, s.Cell(113, 3)), Is.False);
        }

        [Test]
        public void AmphibiousAssault_IsWeakerThanTheSameAttackOverLand()
        {
            Fixed64 Defended(bool fromSea)
            {
                var s = new StraitScenario();
                LandUnit defender = s.Unit(TestWorlds.CountryB, 113, 3);
                HexCoord port = StraitScenario.At(s.PortA, s.State.World);
                Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
                var attackers = new List<LandUnit> { s.Unit(TestWorlds.CountryA, 109, 4), s.Unit(TestWorlds.CountryA, 109, 4) };
                var rng = new RngStream(5, RngSubsystem.Combat, 1, 1, 1);
                if (fromSea)
                {
                    AmphibiousRules.Embark(s.War, attackers[0], transports);
                    AmphibiousRules.Embark(s.War, attackers[1], transports);
                    transports.Hex = s.Hex(112, 3);
                    attackers[0].Hex = attackers[1].Hex = transports.Hex;
                    CombatRules.ResolveLanding(s.War, attackers, new HexCoord(113, 3), ref rng, new List<string>());
                }
                else
                {
                    attackers[0].Hex = attackers[1].Hex = s.Hex(114, 3);
                    CombatRules.Resolve(s.War, attackers, new HexCoord(113, 3), ref rng, new List<string>());
                }

                return defender.Organization;
            }

            Assert.That(Defended(true) > Defended(false), Is.True, "çıkarma ×0,5 güçle saldırır");
        }

        private static (StraitScenario S, Fleet Transports, LandUnit Cargo, Fleet Battleships) Convoy(ulong seed)
        {
            var s = new StraitScenario(seed);
            HexCoord port = StraitScenario.At(s.PortA, s.State.World);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
            LandUnit cargo = s.Unit(TestWorlds.CountryA, 109, 4);
            AmphibiousRules.Embark(s.War, cargo, transports);
            transports.Hex = cargo.Hex = s.Hex(111, 4);
            return (s, transports, cargo, s.Fleet(TestWorlds.CountryB, BaselineUnits.BattleshipSquadron, 111, 4));
        }

        private static void Fight(StraitScenario s, ulong seed, int days, List<string> notes)
        {
            for (int day = 0; day < days; day++)
            {
                var hexes = new List<HexId>();
                NavalCombatRules.ContestedHexes(s.War, hexes);
                for (int i = 0; i < hexes.Count; i++)
                {
                    var rng = new RngStream(seed, RngSubsystem.NavalCombat, day, hexes[i].Value, 0);
                    NavalCombatRules.ResolveBattle(s.War, hexes[i], ref rng, notes);
                }
            }
        }

        [Test]
        public void NavalBattle_TransportsCannotHurtBattleships_AndWithdrawWithTheirCargo()
        {
            for (ulong seed = 1; seed <= 10; seed++)
            {
                (StraitScenario s, Fleet transports, LandUnit cargo, Fleet battleships) = Convoy(seed);
                var hexes = new List<HexId>();
                NavalCombatRules.ContestedHexes(s.War, hexes);
                Assert.That(hexes, Is.EqualTo(new[] { s.Hex(111, 4) }));
                Fight(s, seed, 5, new List<string>());
                Assert.That(battleships.Organization, Is.EqualTo(StraitScenario.Content.Warfare.ShipTypes[BaselineUnits.BattleshipSquadron].MaxOrganization), "nakliye karşılık veremez");
                Assert.That(s.State.Warfare.Fleets.ContainsKey(transports.Id), Is.True);
                Assert.That(transports.Hex, Is.Not.EqualTo(s.Hex(111, 4)), "kırılan nakliye çekilir");
                Assert.That(s.State.World.Cell(transports.Hex).IsWater, Is.True);
                Assert.That(cargo.Hex, Is.EqualTo(transports.Hex), "yük filoyla çekilir");
                Assert.That(transports.Equipment < Fixed64.One, Is.True, "gemi kaybı");
                AssertInvariants(s);
            }
        }

        [Test]
        public void NavalBattle_SurroundedTransportsSink_WithTheTroopsAboard()
        {
            (StraitScenario s, Fleet transports, LandUnit cargo, Fleet battleships) = Convoy(7);
            HexCoord here = new HexCoord(111, 4);
            for (int d = 0; d < 6; d++)
            {
                s.State.World.Grid.TryGetNeighbor(here, (HexDirection)d, out HexCoord n);
                s.Fleet(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, n.Column, n.Row);
            }

            long westBefore = s.State.Population.Regions[s.West].Total;
            var notes = new List<string>();
            Fight(s, 7, 5, notes);
            Assert.That(s.State.Warfare.Fleets.ContainsKey(transports.Id), Is.False, "kaçacak deniz yok → batar");
            Assert.That(s.State.Warfare.Units.ContainsKey(cargo.Id), Is.False, "gemideki tümen kaybedilir");
            Assert.That(notes, Has.Some.StartsWith(FleetRules.SunkNote));
            Assert.That(s.State.Population.Regions[s.West].Total, Is.LessThan(westBefore - 9_000));
            Assert.That(s.State.Warfare.Fleets.ContainsKey(battleships.Id), Is.True);
            AssertInvariants(s);
        }

        [Test]
        public void Blockade_ClosesThePort_AndOnlyThenCompletesTheSiege()
        {
            var s = new StraitScenario();
            HexCoord portA = StraitScenario.At(s.PortA, s.State.World);
            for (int d = 0; d < 6; d++)
            {
                if (s.State.World.Grid.TryGetNeighbor(portA, (HexDirection)d, out HexCoord n) && !s.State.World.Cell(n).IsWater)
                {
                    s.State.World.SetHexController(n, TestWorlds.CountryB);
                }
            }

            WarfareControlRules.UpdateBlockades(s.War);
            WarfareControlRules.UpdateSieges(s.War);
            Assert.That(s.State.Warfare.IsBesieged(s.PortA.Id), Is.False, "deniz yolu açık liman kuşatılmış sayılmaz");

            Fleet raider = s.Fleet(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, 110, 4);
            WarfareControlRules.UpdateBlockades(s.War);
            WarfareControlRules.UpdateSieges(s.War);
            Assert.That(s.State.Warfare.IsBlockaded(s.PortA.Id), Is.True);
            Assert.That(s.State.Warfare.IsBesieged(s.PortA.Id), Is.True);

            s.Fleet(TestWorlds.CountryA, BaselineUnits.DestroyerFlotilla, portA.Column, portA.Row);
            WarfareControlRules.UpdateBlockades(s.War);
            WarfareControlRules.UpdateSieges(s.War);
            Assert.That(s.State.Warfare.IsBlockaded(s.PortA.Id), Is.False, "limandaki kendi filosu ablukayı kırar");
            Assert.That(s.State.Warfare.IsBesieged(s.PortA.Id), Is.False);
            Assert.That(raider.Hex, Is.EqualTo(s.Hex(110, 4)));
            AssertInvariants(s);
        }

        [Test]
        public void Convoy_SuppliesAnOccupiedPort_OnlyWithIdleTransportsAndAnOpenSeaLane()
        {
            var s = new StraitScenario();
            Fleet blocker = s.Fleet(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, 300, 4);

            s.Occupy(s.PortB, TestWorlds.CountryA);
            var sources = new List<SupplySource>();
            ConvoyRules.CollectSources(s.War, TestWorlds.CountryA, sources);
            Assert.That(sources.Count, Is.EqualTo(2), "yalnız iki ana şehir; işgal limanı kendiliğinden kaynak değil");

            HexCoord portA = StraitScenario.At(s.PortA, s.State.World);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, portA.Column, portA.Row);
            Assert.That(ConvoyRules.ConvoyCapacity(s.War, TestWorlds.CountryA), Is.EqualTo(2));
            sources.Clear();
            ConvoyRules.CollectSources(s.War, TestWorlds.CountryA, sources);
            Assert.That(sources.Count, Is.EqualTo(3));
            Assert.That(sources[2].Coord, Is.EqualTo(StraitScenario.At(s.PortB, s.State.World)));
            Assert.That(sources[2].Level, Is.EqualTo(Fixed64.Parse("0.7")));

            // A limanının tek deniz komşusu (110,4) düşman filosuyla tutulursa deniz yolu kesilir
            // (harita sarmalandığı için boğazı tek sütunla kapatmak yetmez: yol dünyayı dolaşır).
            blocker.Hex = s.Hex(110, 4);

            sources.Clear();
            ConvoyRules.CollectSources(s.War, TestWorlds.CountryA, sources);
            Assert.That(sources.Count, Is.EqualTo(2), "deniz yolu kesik");

            blocker.Hex = s.Hex(111, 4);
            sources.Clear();
            ConvoyRules.CollectSources(s.War, TestWorlds.CountryA, sources);
            Assert.That(sources.Count, Is.EqualTo(3), "boğazdaki tek filo yolu kesemez: konvoy etrafından dolaşır");

            transports.Hex = s.Hex(110, 4);
            Assert.That(ConvoyRules.ConvoyCapacity(s.War, TestWorlds.CountryA), Is.EqualTo(0), "denizdeki nakliye konvoy sayılmaz");

            var grid = SupplyRules.Compute(s.State.World, new LandMovementCostModel(StraitScenario.Content.Movement), OwnTerritorySupplyAccess.Instance,
                TestWorlds.CountryA, new[] { new SupplySource(StraitScenario.At(s.PortB, s.State.World), Fixed64.Parse("0.7")) }, Fixed64.FromInt(400));
            Assert.That(grid.Level(s.State.World.Grid.IndexOf(StraitScenario.At(s.PortB, s.State.World))), Is.EqualTo(Fixed64.Parse("0.7")));
        }

        [Test]
        public void CapturedPort_PushesEnemyFleetsOutToSea()
        {
            var s = new StraitScenario();
            HexCoord portB = StraitScenario.At(s.PortB, s.State.World);
            Fleet docked = s.Fleet(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, portB.Column, portB.Row);
            s.Occupy(s.PortB, TestWorlds.CountryA);
            var notes = new List<string>();
            FleetRules.EvictFromPort(s.War, s.PortB, TestWorlds.CountryA, notes);
            Assert.That(s.State.World.Cell(docked.Hex).IsWater, Is.True);
            AssertInvariants(s);
        }

        [Test]
        public void Snapshot_RoundTripsFleetsWingsAndCargo()
        {
            var s = new StraitScenario();
            HexCoord port = StraitScenario.At(s.PortA, s.State.World);
            Fleet transports = s.Fleet(TestWorlds.CountryA, BaselineUnits.TransportFlotilla, port.Column, port.Row);
            LandUnit cargo = s.Unit(TestWorlds.CountryA, 109, 4);
            AmphibiousRules.Embark(s.War, cargo, transports);
            transports.Hex = cargo.Hex = s.Hex(111, 7);
            FleetRules.SetPath(transports, new[] { s.Hex(111, 6) });
            AirWing wing = s.Wing(TestWorlds.CountryA, BaselineUnits.BomberWing, s.InlandA);
            AirRules.SetMission(wing, AirMission.GroundSupport, s.Hex(108, 4));
            s.Fleet(TestWorlds.CountryB, BaselineUnits.DestroyerFlotilla, 110, 4);
            WarfareControlRules.UpdateBlockades(s.War);
            Assert.That(s.State.Warfare.Blockaded.Count, Is.EqualTo(1));

            var w = new CanonicalWriter();
            WarfareSnapshotCodec.Write(s.State.Warfare, w);
            Result<WarfareState> back = WarfareSnapshotCodec.Read(new CanonicalReader(w.ToArray()));
            Assert.That(back.IsSuccess, Is.True);
            var w2 = new CanonicalWriter();
            WarfareSnapshotCodec.Write(back.Value, w2);
            Assert.That(w2.ToArray(), Is.EqualTo(w.ToArray()));
            Assert.That(back.Value.Units.ValueAt(0).Carrier, Is.EqualTo(transports.Id));
            Assert.That(back.Value.AirWings.ValueAt(0).Mission, Is.EqualTo(AirMission.GroundSupport));
        }
    }
}
