using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>Adım 12/1: ulaşım grafında ikmal, coğrafi toplama ve çok günlü hareket (REG-002, WAR-L11/L12).</summary>
    [TestFixture]
    public class GeoSupplyAndMovementTests
    {
        private static readonly Fixed64 Range = Fixed64.FromInt(3_000);

        [Test]
        public void Supply_FlowsThroughControlledRegions_AndFadesWithCost()
        {
            var f = new GeoWarFixture();
            GeoSupplyNetwork net = Compute(f, Red);
            Assert.That(net.NodeLevel(A1), Is.EqualTo(Fixed64.One));
            Assert.That(net.NodeLevel(A2), Is.EqualTo(Fixed64.One - Fixed64.FromRatio(800, 3_000)), "40 km yol × 20");
            Assert.That(net.NodeLevel(B1), Is.EqualTo(Fixed64.Zero), "düşman bölgesi ikmali keser");
            Assert.That(net.NodeLevel(CP), Is.EqualTo(Fixed64.One), "ada şehri kendi kaynağı");
            Assert.That(net.RegionLevel(A), Is.EqualTo(Fixed64.One));

            f.SetController(B, Red);
            Assert.That(Compute(f, Red).NodeLevel(B1).IsPositive, Is.True, "işgal edilen bölgeden ikmal geçer");
        }

        [Test]
        public void Supply_Railway_ExtendsReach_BesiegedCityIsNoSource()
        {
            var f = new GeoWarFixture();
            f.SetController(B, Red);
            Fixed64 byRoad = Compute(f, Red).NodeLevel(B1);
            f.Today = Day1935;
            Fixed64 byRail = Compute(f, Red).NodeLevel(B1);
            Assert.That(byRail, Is.GreaterThan(byRoad));

            f.World.TryGetCity(GeoWarFixture.CityA1, out City a1);
            f.World.TryGetCity(GeoWarFixture.CityAP, out City ap);
            a1.Siege = SiegeState.Besieged;
            ap.Siege = SiegeState.Besieged;
            List<GeoSupplySource> sources = GeoSupplyRules.HomeSources(f.World, Red);
            Assert.That(sources.Count, Is.EqualTo(1), "yalnız ada limanı");
            Assert.That(Compute(f, Red).NodeLevel(A1), Is.EqualTo(Fixed64.Zero));
        }

        [Test]
        public void Recruit_OnlyInOwnControlledCity_DrawsPopulationAndEquipment()
        {
            var f = new GeoWarFixture();
            Fixed64 stockBefore = f.Economy.Countries[Red].Inventory[BaselineGoods.InfantryEquipment];
            long mobilizedBefore = f.Population.Regions[A].Mobilized;
            LandUnit unit = f.Recruit(Red, GeoWarFixture.CityA1);
            Assert.That(unit.Node, Is.EqualTo(A1));
            Assert.That(f.Population.Regions[A].Mobilized - mobilizedBefore, Is.EqualTo(10_000L));
            Assert.That(f.Economy.Countries[Red].Inventory[BaselineGoods.InfantryEquipment], Is.EqualTo(stockBefore - Fixed64.FromInt(100)));
            Assert.That(GeoUnitRules.CheckRecruit(f.Ctx, Red, BaselineUnits.InfantryDivision, GeoWarFixture.CityB1).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.ToplamaYeriGecersiz));

            f.World.TryGetCity(GeoWarFixture.CityA1, out City a1);
            a1.Controller = Blue;
            Assert.That(GeoUnitRules.CheckRecruit(f.Ctx, Red, BaselineUnits.InfantryDivision, GeoWarFixture.CityA1).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.ToplamaYeriGecersiz), "işgal altındaki kendi şehri");
        }

        [Test]
        public void Movement_TakesDays_AndKeepsIntermediatePosition()
        {
            var f = new GeoWarFixture();
            LandUnit unit = f.Recruit(Red, GeoWarFixture.CityA1);
            Order(f, unit, TargetRef.AtPoint(P("40.55", "28.88")));
            Assert.That(unit.Route, Is.EqualTo(new[] { new TransportEdgeId(1UL) }), "noktaya en yakın düğüm a2");

            var notes = new List<string>();
            GeoMovementRules.AdvanceDay(f.Ctx, notes);
            Assert.That(unit.Node, Is.EqualTo(A1), "800'lük yolun 600'ü");
            Assert.That(unit.RouteProgress, Is.EqualTo(600L));
            GeoMovementRules.AdvanceDay(f.Ctx, notes);
            Assert.That(unit.Node, Is.EqualTo(A2));
            Assert.That(unit.Route, Is.Empty);
            Assert.That(unit.RouteProgress, Is.EqualTo(0L));
        }

        [Test]
        public void Movement_NoWalkingToIslands_NoNeutralTerritory_NoMarchIntoEnemyCity()
        {
            var f = new GeoWarFixture();
            LandUnit unit = f.Recruit(Red, GeoWarFixture.CityA1);
            var route = new List<TransportEdgeId>();
            var finder = new TransportPathfinder(f.World.Transport);

            Assert.That(GeoMovementRules.CheckMove(f.Ctx, finder, Red, unit.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityCP.Value), route).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.RotaYok), "deniz üstünden yürüyüş yok");
            Assert.That(GeoMovementRules.CheckMove(f.Ctx, finder, Red, unit.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value), route).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.HedefGecersiz), "düşman şehrine saldırı emri gerekir");
            Assert.That(GeoMovementRules.CheckMove(f.Ctx, finder, Blue, unit.Id, TargetRef.AtPoint(P("40.5", "28.9")), route).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.BirlikBizimDegil));

            f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
            b1.Controller = GeoWarFixture.Neutral;
            b1.Owner = GeoWarFixture.Neutral;
            f.SetController(B, GeoWarFixture.Neutral);
            Assert.That(GeoMovementRules.CheckMove(f.Ctx, finder, Red, unit.Id, TargetRef.Entity(TargetKind.Region, B.Value), route).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.RotaYok), "tarafsız toprak geçiş hakkı olmadan kapalı");
        }

        [Test]
        public void Movement_StopsOnContact_BeforeEnemyOccupiedNode()
        {
            var f = new GeoWarFixture();
            LandUnit enemy = f.Recruit(Blue, GeoWarFixture.CityB1);
            enemy.Node = A2;
            f.SetController(B, Red);
            f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
            b1.Controller = Red;
            LandUnit unit = f.Recruit(Red, GeoWarFixture.CityA1);
            Order(f, unit, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
            Assert.That(unit.Route, Is.EqualTo(new[] { new TransportEdgeId(1UL), new TransportEdgeId(2UL) }), "rota yolu düşmanlı kavşaktan geçer");
            var notes = new List<string>();
            GeoMovementRules.AdvanceDay(f.Ctx, notes);
            Assert.That(unit.Node, Is.EqualTo(A1), "düşmanlı düğüme girmez");
            Assert.That(unit.Route, Is.Empty);
            Assert.That(notes, Has.Some.StartsWith(GeoMovementRules.ContactNote));
        }

        [Test]
        public void Movement_IsDeterministic()
        {
            long[] Run()
            {
                var f = new GeoWarFixture(Day1935);
                f.SetController(B, Red);
                f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
                b1.Controller = Red;
                LandUnit u1 = f.Recruit(Red, GeoWarFixture.CityA1);
                LandUnit u2 = f.Recruit(Red, GeoWarFixture.CityAP);
                Order(f, u1, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
                Order(f, u2, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
                var notes = new List<string>();
                var trace = new List<long>();
                for (int d = 0; d < 4; d++)
                {
                    GeoMovementRules.AdvanceDay(f.Ctx, notes);
                    trace.Add((long)u1.Node.Value * 1_000_000L + u1.RouteProgress);
                    trace.Add((long)u2.Node.Value * 1_000_000L + u2.RouteProgress);
                }

                return trace.ToArray();
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }

        private static void Order(GeoWarFixture f, LandUnit unit, TargetRef target)
        {
            var route = new List<TransportEdgeId>();
            var check = GeoMovementRules.CheckMove(f.Ctx, new TransportPathfinder(f.World.Transport), unit.Owner, unit.Id, target, route);
            Assert.That(check.IsValid, Is.True, check.ErrorKey);
            GeoMovementRules.SetRoute(unit, route);
        }

        private static GeoSupplyNetwork Compute(GeoWarFixture f, CountryId country) =>
            GeoSupplyRules.Compute(f.World, new EdgeKindCostModel(LandCosts(), false, f.Today), new ControlledRegionSupplyAccess(f.World), country,
                GeoSupplyRules.HomeSources(f.World, country), Range);
    }
}
