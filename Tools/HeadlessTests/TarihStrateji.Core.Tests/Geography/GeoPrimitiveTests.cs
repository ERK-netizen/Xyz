using System;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    [TestFixture]
    public class GeoPrimitiveTests
    {
        [Test]
        public void Longitude_IsNormalizedToHalfOpenRange()
        {
            Assert.That(new GeoPoint(0, 180_000_000).LongitudeE6, Is.EqualTo(180_000_000));
            Assert.That(new GeoPoint(0, -180_000_000).LongitudeE6, Is.EqualTo(180_000_000), "±180 aynı meridyen");
            Assert.That(new GeoPoint(0, 190_000_000).LongitudeE6, Is.EqualTo(-170_000_000));
            Assert.That(GeoPoint.LongitudeDelta(179_000_000, -179_000_000), Is.EqualTo(2_000_000), "dikiş üzerinden kısa yön");
            Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(90_000_001, 0));
        }

        [Test]
        public void Distance_MatchesKnownCityPairs_AndIsSymmetric()
        {
            var istanbul = P("41.0082", "28.9784");
            var ankara = P("39.9334", "32.8597");
            var london = P("51.5074", "-0.1278");
            var newYork = P("40.7128", "-74.0060");

            long ia = GeoMath.DistanceMeters(istanbul, ankara);
            Assert.That(ia, Is.InRange(348_000L, 354_000L));
            Assert.That(GeoMath.DistanceMeters(ankara, istanbul), Is.EqualTo(ia));
            Assert.That(GeoMath.DistanceMeters(london, newYork), Is.InRange(5_560_000L, 5_580_000L));
            Assert.That(GeoMath.DistanceMeters(istanbul, istanbul), Is.EqualTo(0L));
        }

        [Test]
        public void Distance_AcrossAntimeridian_IsShort()
        {
            long d = GeoMath.DistanceMeters(P("0", "179.5"), P("0", "-179.5"));
            Assert.That(d, Is.InRange(110_000L, 112_500L), "dikişte dünyayı dolaşmaz");
        }

        [Test]
        public void Distance_IsBitExactAcrossCalls()
        {
            // Determinizm: aynı girdi her zaman aynı metre (kayan nokta yok).
            var a = P("37.0", "35.3");
            var b = P("41.3", "19.8");
            long first = GeoMath.DistanceMeters(a, b);
            for (int i = 0; i < 100; i++)
            {
                Assert.That(GeoMath.DistanceMeters(a, b), Is.EqualTo(first));
            }
        }

        [Test]
        public void TargetRef_RejectsEmptyEntities()
        {
            Assert.Throws<ArgumentException>(() => TargetRef.Entity(TargetKind.City, 0UL));
            Assert.Throws<ArgumentException>(() => TargetRef.Entity(TargetKind.Point, 5UL));
            Assert.That(TargetRef.None.IsValid, Is.False);
            Assert.That(TargetRef.Entity(TargetKind.LandUnit, 7UL).IsValid, Is.True);
            Assert.That(TargetRef.AtPoint(P("40", "29")).IsValid, Is.True);
            Assert.That(TargetRef.Entity(TargetKind.City, 3UL), Is.Not.EqualTo(TargetRef.Entity(TargetKind.LandUnit, 3UL)));
        }
    }
}
