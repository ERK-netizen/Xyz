using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Geo;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>Adım 12/3: filo, deniz muharebesi, abluka, çıkarma, konvoy, hava menzili, ortak destek, şehir slotu (REG-006/011).</summary>
    [TestFixture]
    public class GeoNavalAirTests
    {
        private const ulong Seed = 1914UL;

        [Test]
        public void Fleet_IsBuiltOnlyInHomePort_AndSailsSeaLanesOverDays()
        {
            var f = new GeoWarFixture();
            Assert.That(GeoNavalRules.CheckBuild(f.Ctx, Red, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityA1).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.LimanGecersiz), "a1 liman değil");
            Fleet fleet = Build(f, Red, BaselineUnits.TransportFlotilla, GeoWarFixture.CityAP);
            Assert.That(fleet.Node, Is.EqualTo(AP));

            var route = new List<TransportEdgeId>();
            var finder = new TransportPathfinder(f.World.Transport);
            Assert.That(GeoNavalRules.CheckMove(f.Ctx, finder, Red, fleet.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityCP.Value), route).IsValid, Is.True);
            Assert.That(route, Is.EqualTo(new[] { new TransportEdgeId(5UL), new TransportEdgeId(6UL) }));
            Assert.That(GeoNavalRules.CheckMove(f.Ctx, finder, Red, fleet.Id, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value), route).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.HedefGecersiz), "kara şehrine filo giremez");
        }

        [Test]
        public void Fleet_StopsOnContact_AndBattlesDeterministically()
        {
            string Run()
            {
                var f = new GeoWarFixture();
                Fleet red = Build(f, Red, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
                Fleet blue = Build(f, Blue, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
                blue.Node = S;
                Order(f, red, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityCP.Value));
                var notes = new List<string>();
                GeoNavalRules.AdvanceDay(f.Ctx, notes);
                Assert.That(red.Node, Is.EqualTo(S));
                Assert.That(red.Route, Is.Empty, "temas");
                GeoNavalRules.ResolveBattles(f.Ctx, Seed, notes);
                Assert.That(notes, Has.Some.StartsWith(NavalCombatRules.BattleNote));
                return red.Organization.Raw + "|" + blue.Organization.Raw;
            }

            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void Blockade_HostileFleetOffPort_UnlessDefenderPresent()
        {
            var f = new GeoWarFixture();
            Fleet blue = Build(f, Blue, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
            blue.Node = S;
            GeoNavalRules.UpdateBlockades(f.Ctx);
            Assert.That(f.Warfare.IsBlockaded(GeoWarFixture.CityAP), Is.True);
            Assert.That(f.Warfare.IsBlockaded(GeoWarFixture.CityCP), Is.True);

            f.World.TryGetCity(GeoWarFixture.CityCP, out City cp);
            GeoSiegeRules.Update(f.Ctx);
            Assert.That(cp.Siege, Is.EqualTo(SiegeState.Besieged), "kara komşusuz ada yalnız ablukayla kuşatılır");

            Build(f, Red, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
            GeoNavalRules.UpdateBlockades(f.Ctx);
            Assert.That(f.Warfare.IsBlockaded(GeoWarFixture.CityAP), Is.False, "limanda kendi filosu var");
        }

        [Test]
        public void Embark_RequiresTransportCapacity_REG006()
        {
            var f = new GeoWarFixture();
            Fleet destroyer = Build(f, Red, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
            Fleet transport = Build(f, Red, BaselineUnits.TransportFlotilla, GeoWarFixture.CityAP);
            var units = new[] { f.Place(Red, GeoWarFixture.CityAP, AP), f.Place(Red, GeoWarFixture.CityAP, AP), f.Place(Red, GeoWarFixture.CityAP, AP) };
            Assert.That(GeoAmphibiousRules.CheckEmbark(f.Ctx, Red, units[0].Id, destroyer.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.NakliyeKapasitesiYok));
            for (int i = 0; i < 2; i++)
            {
                Assert.That(GeoAmphibiousRules.CheckEmbark(f.Ctx, Red, units[i].Id, transport.Id).IsValid, Is.True);
                GeoAmphibiousRules.Embark(units[i], transport);
            }

            Assert.That(GeoAmphibiousRules.CheckEmbark(f.Ctx, Red, units[2].Id, transport.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.NakliyeKapasitesiDolu));
            LandUnit ashore = f.Place(Red, GeoWarFixture.CityA1, A1);
            Assert.That(GeoAmphibiousRules.CheckEmbark(f.Ctx, Red, ashore.Id, transport.Id).ErrorKey, Is.EqualTo(WarfareErrorKeys.AyniLimandaDegil));
            Assert.That(GeoMovementRules.CheckMove(f.Ctx, new TransportPathfinder(f.World.Transport), Red, units[0].Id, TargetRef.AtPoint(P("40.5", "28.9")), new List<TransportEdgeId>()).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.BirlikGemide));
        }

        [Test]
        public void Landing_OnOwnIslandPort_AfterSeaVoyage_AndCargoSinksWithFleet()
        {
            var f = new GeoWarFixture();
            Fleet transport = Build(f, Red, BaselineUnits.TransportFlotilla, GeoWarFixture.CityAP);
            LandUnit unit = f.Place(Red, GeoWarFixture.CityAP, AP);
            GeoAmphibiousRules.Embark(unit, transport);
            transport.Node = S;
            GeoNavalRules.SyncCargo(f.Ctx, transport);
            Assert.That(GeoAmphibiousRules.CheckLanding(f.Ctx, Red, unit.Id, GeoWarFixture.CityCP).IsValid, Is.True);
            Assert.That(GeoAmphibiousRules.CheckLanding(f.Ctx, Red, unit.Id, GeoWarFixture.CityB1).ErrorKey, Is.EqualTo(WarfareErrorKeys.CikarmaHedefiGecersiz));
            GeoAmphibiousRules.SetLanding(unit, GeoWarFixture.CityCP);
            GeoAmphibiousRules.ResolveLandings(f.Ctx, Seed, new List<string>());
            Assert.That(unit.IsEmbarked, Is.False);
            Assert.That(unit.Node, Is.EqualTo(CP));

            LandUnit second = f.Place(Red, GeoWarFixture.CityAP, AP);
            transport.Node = AP;
            GeoAmphibiousRules.Embark(second, transport);
            GeoNavalRules.Sink(f.Ctx, transport, new List<string>());
            Assert.That(f.Warfare.Units.ContainsKey(second.Id), Is.False, "batan filo taşıdığı birliği de kaybeder");
        }

        [Test]
        public void Landing_OnHostilePort_IsAnAmphibiousAssault()
        {
            var f = new GeoWarFixture();
            Fleet transport = Build(f, Red, BaselineUnits.TransportFlotilla, GeoWarFixture.CityAP);
            var units = new[] { f.Place(Red, GeoWarFixture.CityAP, AP), f.Place(Red, GeoWarFixture.CityAP, AP) };
            foreach (LandUnit u in units)
            {
                GeoAmphibiousRules.Embark(u, transport);
            }

            transport.Node = S;
            GeoNavalRules.SyncCargo(f.Ctx, transport);
            f.World.TryGetCity(GeoWarFixture.CityCP, out City cp);
            cp.Controller = Blue;
            var notes = new List<string>();
            foreach (LandUnit u in units)
            {
                GeoAmphibiousRules.SetLanding(u, GeoWarFixture.CityCP);
            }

            GeoAmphibiousRules.ResolveLandings(f.Ctx, Seed, notes);
            Assert.That(notes, Has.Some.StartsWith(CombatRules.BattleNote).Or.Some.StartsWith(CombatRules.CityCapturedNote), "garnizona çıkarma muharebesi");
            Assert.That(cp.Owner, Is.EqualTo(Red), "sahiplik değişmez");
        }

        [Test]
        public void Convoy_SuppliesOccupiedPort_OnlyThroughFreeSeaLanes()
        {
            var f = new GeoWarFixture();
            f.World.TryGetCity(GeoWarFixture.CityCP, out City cp);
            cp.Owner = Blue;
            Build(f, Red, BaselineUnits.TransportFlotilla, GeoWarFixture.CityAP);
            var sources = new List<GeoSupplySource>();
            GeoConvoyRules.CollectSources(f.Ctx, Red, sources);
            Assert.That(sources.Count, Is.EqualTo(1));
            Assert.That(sources[0].Node, Is.EqualTo(CP));
            Assert.That(sources[0].Level, Is.EqualTo(Fixed64.Parse("0.7")));

            Fleet blue = Build(f, Blue, BaselineUnits.DestroyerFlotilla, GeoWarFixture.CityAP);
            blue.Node = S;
            sources.Clear();
            GeoConvoyRules.CollectSources(f.Ctx, Red, sources);
            Assert.That(sources, Is.Empty, "düşman filosu deniz yolunu keser");
        }

        [Test]
        public void AirMission_RangeIsGreatCircleFromBase_REG011()
        {
            var f = new GeoWarFixture();
            Result<AirWingId> id = GeoAirRules.Recruit(f.Ctx, Red, BaselineUnits.FighterWing, GeoWarFixture.CityA1);
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            Assert.That(GeoAirRules.CheckMission(f.Ctx, Red, id.Value, AirMission.AirSuperiority, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value)).IsValid, Is.True);
            Assert.That(GeoAirRules.CheckMission(f.Ctx, Red, id.Value, AirMission.AirSuperiority, TargetRef.AtPoint(P("45", "40"))).ErrorKey,
                Is.EqualTo(WarfareErrorKeys.MenzilDisi));
            Assert.That(GeoAirRules.CheckRecruit(f.Ctx, Red, BaselineUnits.FighterWing, GeoWarFixture.CityB1).ErrorKey, Is.EqualTo(WarfareErrorKeys.HavaUssuGecersiz));
        }

        [Test]
        public void GroundSupport_AddsPowerAtTargetNode_ScaledByAirShare()
        {
            var f = new GeoWarFixture();
            AirWingId bomber = GeoAirRules.Recruit(f.Ctx, Red, BaselineUnits.BomberWing, GeoWarFixture.CityA1).Value;
            GeoAirRules.SetMission(f.Warfare.AirWings[bomber], AirMission.GroundSupport, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
            Fixed64 alone = GeoJointSupportRules.Support(f.Ctx, B1, Red);
            Assert.That(alone.IsPositive, Is.True);
            Assert.That(GeoJointSupportRules.Support(f.Ctx, A2, Red), Is.EqualTo(Fixed64.Zero), "destek yalnız hedef düğümde");

            AirWingId fighter = GeoAirRules.Recruit(f.Ctx, Blue, BaselineUnits.FighterWing, GeoWarFixture.CityB1).Value;
            GeoAirRules.SetMission(f.Warfare.AirWings[fighter], AirMission.AirSuperiority, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
            Assert.That(GeoJointSupportRules.Support(f.Ctx, B1, Red), Is.LessThan(alone), "düşman hava üstünlüğü desteği düşürür");
        }

        [Test]
        public void CitySlots_ComeFromDevelopment_OnlyForOwnerAndController()
        {
            var f = new GeoWarFixture();
            f.World.TryGetCity(GeoWarFixture.CityA1, out City a1);
            Assert.That(CitySlotRules.Capacity(a1, 4), Is.EqualTo(20));
            Assert.That(CitySlotRules.CheckBuild(a1, Red, 18, 2, 4).IsValid, Is.True);
            Assert.That(CitySlotRules.CheckBuild(a1, Red, 19, 2, 4).ErrorKey, Is.EqualTo(TarihStrateji.Core.Economy.EconomyErrorKeys.SlotYetersiz));
            a1.Controller = Blue;
            Assert.That(CitySlotRules.CheckBuild(a1, Red, 0, 1, 4).ErrorKey, Is.EqualTo(TarihStrateji.Core.Economy.EconomyErrorKeys.SektorBizimDegil), "işgalde inşaat yok");
        }

        private static Fleet Build(GeoWarFixture f, CountryId country, UnitTypeId type, CityId port)
        {
            f.World.TryGetCity(port, out City city);
            CountryId owner = city.Owner;
            CountryId controller = city.Controller;
            city.Owner = country;
            city.Controller = country;
            Result<FleetId> id = GeoNavalRules.Build(f.Ctx, country, type, port);
            city.Owner = owner;
            city.Controller = controller;
            Assert.That(id.IsSuccess, Is.True, id.ErrorKey);
            Fleet fleet = f.Warfare.Fleets[id.Value];
            fleet.Organization = BaselineContent.Create().Warfare.ShipTypes[type].MaxOrganization;
            return fleet;
        }

        private static void Order(GeoWarFixture f, Fleet fleet, TargetRef target)
        {
            var route = new List<TransportEdgeId>();
            Assert.That(GeoNavalRules.CheckMove(f.Ctx, new TransportPathfinder(f.World.Transport), fleet.Owner, fleet.Id, target, route).IsValid, Is.True);
            GeoNavalRules.SetRoute(fleet, route);
        }
    }
}
