using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>CITY-001/002/005/008, MAP-G02/G07/G08, REG-030: sektörsüz şehir ve bölge kontrol aşamaları.</summary>
    [TestFixture]
    public class CityAndControlTests
    {
        private static readonly RegionControlParameters Params = new RegionControlParameters(3, 5);

        [Test]
        public void City_MustLieInItsRegion_AndOnAMatchingNode()
        {
            GeoWorld world = Build();
            Ok(world.AddCity(new City(new CityId(1UL), "sehir.a1", P("40.5", "28.5"), A, A1, 5, Red, Fixed64.One)));
            Assert.That(world.AddCity(new City(new CityId(1UL), "sehir.x", P("40.5", "28.5"), A, A1, 5, Red, Fixed64.One)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.SehirTekrar));
            Assert.That(world.AddCity(new City(new CityId(2UL), "sehir.x", P("40.5", "29.5"), A, B1, 5, Red, Fixed64.One)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.SehirBolgeDisinda));
            Assert.That(world.AddCity(new City(new CityId(3UL), "sehir.x", P("40.5", "28.9"), A, A2, 5, Red, Fixed64.One)).ErrorKey,
                Is.EqualTo(GeographyErrorKeys.SehirDugumUyumsuz), "kavşak şehir düğümü değil");
            Assert.That(world.Cities, Is.EqualTo(new[] { new CityId(1UL) }));
        }

        [Test]
        public void City_SlotsComeFromDevelopment_NotCells()
        {
            var city = new City(new CityId(1UL), "sehir.a1", P("40.5", "28.5"), A, A1, 3, Red, Fixed64.One);
            Assert.That(city.BuildingSlots(4), Is.EqualTo(12));
            city.DevelopmentLevel = 10;
            Assert.That(city.BuildingSlots(4), Is.EqualTo(40));
            Assert.Throws<ArgumentOutOfRangeException>(() => city.DevelopmentLevel = 11);
            Assert.Throws<ArgumentOutOfRangeException>(() => new City(new CityId(2UL), "sehir.x", P("40.5", "28.5"), A, A1, 0, Red, Fixed64.One));
        }

        [Test]
        public void CityOccupation_DoesNotChangeLegalOwner()
        {
            var city = new City(new CityId(1UL), "sehir.a1", P("40.5", "28.5"), A, A1, 5, Red, Fixed64.One);
            city.Controller = Blue;
            Assert.That(city.IsOccupied, Is.True);
            Assert.That(city.Owner, Is.EqualTo(Red));
        }

        [Test]
        public void Port_WithoutSeaLane_FailsWorldValidation()
        {
            GeoWorld world = Build();
            Ok(world.AddCity(new City(new CityId(1UL), "sehir.ap", P("40.1", "28.5"), A, AP, 4, Red, Fixed64.One)));
            Ok(world.Validate());

            var island = new GeoWorld();
            Ok(island.AddRegion(C, Square("38", "28", "39", "29"), Red));
            Ok(island.Transport.AddNode(new TransportNode(CP, P("38.9", "28.5"), C, TransportNodeKind.Port)));
            Ok(island.AddCity(new City(new CityId(2UL), "sehir.cp", P("38.9", "28.5"), C, CP, 2, Red, Fixed64.One)));
            Assert.That(island.Validate().ErrorKey, Is.EqualTo(GeographyErrorKeys.LimanDenizBaglantisiYok));
        }

        [Test]
        public void Region_IsOccupiedOnlyAfterSuppliedHold_OwnerUnchanged()
        {
            var control = new RegionControl(A, Red);
            Assert.That(RegionControlRules.Advance(control, Blue, true, Params), Is.False);
            Assert.That(control.Stage, Is.EqualTo(RegionControlStage.Contested));
            Assert.That(RegionControlRules.Advance(control, Blue, false, Params), Is.False, "ikmalsiz baskı ilerlemez");
            Assert.That(control.DaysHeld, Is.EqualTo(1));
            Assert.That(RegionControlRules.Advance(control, Blue, true, Params), Is.False);
            Assert.That(RegionControlRules.Advance(control, Blue, true, Params), Is.True);
            Assert.That(control.Controller, Is.EqualTo(Blue));
            Assert.That(control.Owner, Is.EqualTo(Red), "işgal ilhak değildir");
            Assert.That(control.Stage, Is.EqualTo(RegionControlStage.Occupied));

            for (int i = 0; i < 4; i++)
            {
                RegionControlRules.Advance(control, Blue, true, Params);
            }

            Assert.That(control.Stage, Is.EqualTo(RegionControlStage.Occupied));
            RegionControlRules.Advance(control, Blue, true, Params);
            Assert.That(control.Stage, Is.EqualTo(RegionControlStage.Secured));
            Assert.That(control.Owner, Is.EqualTo(Red));
        }

        [Test]
        public void ChallengerSwitch_ResetsTheClock_MixedPresenceContestsSecuredRegion()
        {
            var control = new RegionControl(A, Red);
            RegionControlRules.Advance(control, Blue, true, Params);
            RegionControlRules.Advance(control, Blue, true, Params);
            RegionControlRules.Advance(control, new CountryId(3UL), true, Params);
            Assert.That(control.DaysHeld, Is.EqualTo(1));
            Assert.That(control.Challenger, Is.EqualTo(new CountryId(3UL)));

            var calm = new RegionControl(B, Blue);
            RegionControlRules.Advance(calm, CountryId.None, false, Params);
            Assert.That(calm.Stage, Is.EqualTo(RegionControlStage.Contested));
            Assert.That(calm.Controller, Is.EqualTo(Blue));
        }
    }
}
