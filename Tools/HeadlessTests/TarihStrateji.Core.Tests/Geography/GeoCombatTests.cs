using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>Adım 12/2: hedefli muharebe, garnizon, kuşatma, bölge kontrolü (WAR-L01…L10, MAP-G06…G08, CITY-004/005, REG-016/030).</summary>
    [TestFixture]
    public class GeoCombatTests
    {
        private const ulong Seed = 1914UL;

        [Test]
        public void Attack_Validation_OwnershipWarAdjacencyAndPosition()
        {
            var f = new GeoWarFixture();
            LandUnit red = f.Place(Red, GeoWarFixture.CityA1, A2);
            LandUnit blue = f.Place(Blue, GeoWarFixture.CityB1, B1);
            TargetRef atBlue = TargetRef.Entity(TargetKind.LandUnit, blue.Id.Value);

            Assert.That(GeoCombatRules.CheckAttack(f.Ctx, Red, red.Id, atBlue, out TransportNodeId node).IsValid, Is.True);
            Assert.That(node, Is.EqualTo(B1));
            Assert.That(GeoCombatRules.CheckAttack(f.Ctx, Blue, red.Id, atBlue, out _).ErrorKey, Is.EqualTo(WarfareErrorKeys.BirlikBizimDegil), "WAR-L01");

            LandUnit far = f.Place(Red, GeoWarFixture.CityA1, AP);
            Assert.That(GeoCombatRules.CheckAttack(f.Ctx, Red, far.Id, atBlue, out _).ErrorKey, Is.EqualTo(WarfareErrorKeys.HedefKomsuDegil), "WAR-L03");

            red.RouteProgress = 10L;
            Assert.That(GeoCombatRules.CheckAttack(f.Ctx, Red, red.Id, atBlue, out _).ErrorKey, Is.EqualTo(WarfareErrorKeys.HedefKomsuDegil), "kenar ortasında saldırı yok");
            red.RouteProgress = 0L;

            f.Hostility.Wars.Clear();
            Assert.That(GeoCombatRules.CheckAttack(f.Ctx, Red, red.Id, atBlue, out _).ErrorKey, Is.EqualTo(WarfareErrorKeys.SavasYok), "WAR-L02");
        }

        [Test]
        public void Battle_BrokenDefenderWithoutCorridorIsDestroyed_CityStaysWithDefender()
        {
            var f = new GeoWarFixture();
            var attackers = new List<LandUnit> { f.Place(Red, GeoWarFixture.CityA1, A2), f.Place(Red, GeoWarFixture.CityA1, A2), f.Place(Red, GeoWarFixture.CityA1, A2) };
            LandUnit blue = f.Place(Blue, GeoWarFixture.CityB1, B1);
            blue.Organization = Fixed64.FromInt(5);
            long mobilizedBefore = f.Population.Regions[B].Mobilized;
            foreach (LandUnit u in attackers)
            {
                GeoCombatRules.SetAttack(u, TargetRef.Entity(TargetKind.LandUnit, blue.Id.Value));
            }

            var notes = new List<string>();
            GeoCombatRules.ResolveDay(f.Ctx, Seed, new HashSet<ResolutionId>(), notes);
            Assert.That(notes.FindAll(n => n.StartsWith(WarfareNotes.Battle)).Count, Is.EqualTo(1), "aynı hedefe saldıranlar tek muharebede");
            Assert.That(f.Warfare.Units.ContainsKey(blue.Id), Is.False, "çekilme koridoru yok → yok olur");
            Assert.That(f.Population.Regions[B].Mobilized, Is.LessThan(mobilizedBefore), "kayıplar nüfustan düşer (WAR-L08)");
            f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
            Assert.That(b1.Controller, Is.EqualTo(Blue), "şehir ayrıca garnizon muharebesiyle alınır (WAR-L06)");
            Assert.That(attackers[0].Node, Is.EqualTo(A2), "düşman şehrinin düğümüne girilmez");
        }

        [Test]
        public void ImmediateAttack_SpendsActionPoints_AndIsNotResolvedAgainThatDay()
        {
            var f = new GeoWarFixture();
            LandUnit red = f.Place(Red, GeoWarFixture.CityA1, A2);
            red.ActionPoints = Fixed64.FromInt(2);
            LandUnit blue = f.Place(Blue, GeoWarFixture.CityB1, B1);
            TargetRef target = TargetRef.Entity(TargetKind.LandUnit, blue.Id.Value);
            Assert.That(GeoCombatRules.CheckImmediateAttack(f.Ctx, Red, red.Id, target).IsValid, Is.True);

            var resolution = new ResolutionId(1UL);
            var rng = new RngStream(Seed, RngSubsystem.Tactical, f.Today.DayNumber, 1UL, red.Id.Value);
            var notes = new List<string>();
            GeoCombatRules.ResolveImmediate(f.Ctx, red, target, resolution, ref rng, notes);
            Assert.That(red.ActionPoints, Is.EqualTo(Fixed64.One));

            GeoCombatRules.SetAttack(red, target);
            GeoCombatRules.ResolveDay(f.Ctx, Seed, new HashSet<ResolutionId> { resolution }, notes);
            Assert.That(notes.FindAll(n => n.StartsWith(WarfareNotes.Battle)).Count, Is.EqualTo(1), "REG-016");

            red.ActionPoints = Fixed64.Zero;
            Assert.That(GeoCombatRules.CheckImmediateAttack(f.Ctx, Red, red.Id, target).ErrorKey, Is.EqualTo(WarfareErrorKeys.ApYetersiz), "WAR-L04");
        }

        [Test]
        public void CityAssault_ChangesOnlyController_RegionNeedsSuppliedHold()
        {
            var f = new GeoWarFixture();
            var attackers = new List<LandUnit> { f.Place(Red, GeoWarFixture.CityA1, A2), f.Place(Red, GeoWarFixture.CityA1, A2), f.Place(Red, GeoWarFixture.CityA1, A2) };
            f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
            var notes = new List<string>();
            for (int day = 0; day < 20 && b1.Controller == Blue; day++)
            {
                foreach (LandUnit u in attackers)
                {
                    if (f.Warfare.Units.ContainsKey(u.Id))
                    {
                        u.Organization = f.Ctx.Catalog.UnitTypes[u.Type].MaxOrganization;
                        GeoCombatRules.SetAttack(u, TargetRef.Entity(TargetKind.City, GeoWarFixture.CityB1.Value));
                    }
                }

                f.Today = f.Today.AddDays(1);
                GeoCombatRules.ResolveDay(f.Ctx, Seed, new HashSet<ResolutionId>(), notes);
            }

            Assert.That(b1.Controller, Is.EqualTo(Red), "garnizon düştü");
            Assert.That(b1.Owner, Is.EqualTo(Blue), "CITY-005: hukukî sahip değişmez");
            Assert.That(attackers[0].Node, Is.EqualTo(B1));
            f.World.TryGetControl(B, out RegionControl control);
            Assert.That(control.Controller, Is.EqualTo(Blue), "MAP-G08: şehir alımı bölgeyi anında almaz");

            for (int day = 0; day < 3; day++)
            {
                GeoControlRules.UpdateRegions(f.Ctx, Networks(f), notes);
            }

            Assert.That(control.Controller, Is.EqualTo(Red));
            Assert.That(control.Stage, Is.EqualTo(RegionControlStage.Occupied));
            Assert.That(control.Owner, Is.EqualTo(Blue), "REG-030");
        }

        [Test]
        public void Siege_FollowsLandNeighbours()
        {
            var f = new GeoWarFixture();
            f.World.TryGetCity(GeoWarFixture.CityB1, out City b1);
            f.World.TryGetCity(GeoWarFixture.CityA1, out City a1);
            Assert.That(GeoSiegeRules.Evaluate(f.Ctx, b1), Is.EqualTo(SiegeState.Besieged), "b1'in tüm kara komşuları düşman bölgesinde");
            Assert.That(GeoSiegeRules.Evaluate(f.Ctx, a1), Is.EqualTo(SiegeState.Encircling), "demiryolu komşusu b1 düşman bölgesinde");

            f.Hostility.Wars.Clear();
            Assert.That(GeoSiegeRules.Evaluate(f.Ctx, b1), Is.EqualTo(SiegeState.None), "barışta kuşatma yok");
        }

        [Test]
        public void Sustainment_UnsuppliedUnitDecays_SuppliedRecovers()
        {
            var f = new GeoWarFixture();
            LandUnit home = f.Place(Red, GeoWarFixture.CityA1, A2);
            LandUnit deep = f.Place(Red, GeoWarFixture.CityA1, B1);
            home.Organization = Fixed64.FromInt(30);
            deep.Organization = Fixed64.FromInt(30);
            GeoSustainmentRules.ApplyDay(f.Ctx, Networks(f), new List<string>());
            Assert.That(home.Supply.IsPositive, Is.True);
            Assert.That(home.Organization, Is.GreaterThan(Fixed64.FromInt(30)));
            Assert.That(deep.Supply, Is.EqualTo(Fixed64.Zero), "WAR-L05: düşman bölgesinde ikmal yok");
            Assert.That(deep.Organization, Is.LessThan(Fixed64.FromInt(30)));
        }

        [Test]
        public void Combat_IsDeterministicForSameSeed()
        {
            string Run()
            {
                var f = new GeoWarFixture();
                var a = new List<LandUnit> { f.Place(Red, GeoWarFixture.CityA1, A2), f.Place(Red, GeoWarFixture.CityA1, A2) };
                LandUnit blue = f.Place(Blue, GeoWarFixture.CityB1, B1);
                foreach (LandUnit u in a)
                {
                    GeoCombatRules.SetAttack(u, TargetRef.Entity(TargetKind.LandUnit, blue.Id.Value));
                }

                GeoCombatRules.ResolveDay(f.Ctx, Seed, new HashSet<ResolutionId>(), new List<string>());
                return a[0].Organization.Raw + "|" + a[1].Manpower + "|" + (f.Warfare.Units.ContainsKey(blue.Id) ? blue.Organization.Raw.ToString() : "yok");
            }

            Assert.That(Run(), Is.EqualTo(Run()), "WAR-L09");
        }

        internal static Dictionary<CountryId, GeoSupplyNetwork> Networks(GeoWarFixture f)
        {
            var result = new Dictionary<CountryId, GeoSupplyNetwork>();
            foreach (CountryId c in new[] { Red, Blue })
            {
                result[c] = GeoSupplyRules.Compute(f.World, new EdgeKindCostModel(LandCosts(), false, f.Today), new ControlledRegionSupplyAccess(f.World), c,
                    GeoSupplyRules.HomeSources(f.World, c), Fixed64.FromInt(3_000));
            }

            return result;
        }
    }
}
