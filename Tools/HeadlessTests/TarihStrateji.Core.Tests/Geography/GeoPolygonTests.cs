using NUnit.Framework;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;
using static TarihStrateji.Core.Tests.Geography.GeoFixtures;

namespace TarihStrateji.Core.Tests.Geography
{
    /// <summary>MAP-G01: çokgen geçerliliği, delik, ada, sınır noktası, dikiş.</summary>
    [TestFixture]
    public class GeoPolygonTests
    {
        [Test]
        public void Ring_LocatesInsideOutsideAndBoundary()
        {
            GeoRing ring = Box("40", "28", "41", "29");
            Assert.That(ring.Locate(P("40.5", "28.5")), Is.EqualTo(PointLocation.Inside));
            Assert.That(ring.Locate(P("41.5", "28.5")), Is.EqualTo(PointLocation.Outside));
            Assert.That(ring.Locate(P("40", "28.3")), Is.EqualTo(PointLocation.OnBoundary));
            Assert.That(ring.Locate(P("41", "29")), Is.EqualTo(PointLocation.OnBoundary), "köşe");
        }

        [Test]
        public void ConcaveRing_IsHandledExactly()
        {
            // U biçimi: ortadaki girinti dışarıdadır.
            var ring = new GeoRing(new[] { P("0", "0"), P("0", "3"), P("3", "3"), P("3", "2"), P("1", "2"), P("1", "1"), P("3", "1"), P("3", "0") });
            Assert.That(GeometryValidator.ValidateRing(ring).IsValid, Is.True);
            Assert.That(ring.Locate(P("2", "1.5")), Is.EqualTo(PointLocation.Outside));
            Assert.That(ring.Locate(P("2", "0.5")), Is.EqualTo(PointLocation.Inside));
            Assert.That(ring.Locate(P("0.5", "1.5")), Is.EqualTo(PointLocation.Inside));
        }

        [Test]
        public void Polygon_HoleIsOutside_HoleEdgeBelongsToPolygon()
        {
            var polygon = new GeoPolygon(Box("0", "0", "10", "10"), new[] { Box("4", "4", "6", "6") });
            Assert.That(GeometryValidator.ValidatePolygon(polygon).IsValid, Is.True);
            Assert.That(polygon.Locate(P("5", "5")), Is.EqualTo(PointLocation.Outside), "göl/anklav");
            Assert.That(polygon.Locate(P("4", "5")), Is.EqualTo(PointLocation.OnBoundary));
            Assert.That(polygon.Locate(P("2", "2")), Is.EqualTo(PointLocation.Inside));
        }

        [Test]
        public void MultiPolygon_ContainsIslands()
        {
            var multi = new GeoMultiPolygon(new[] { new GeoPolygon(Box("0", "0", "1", "1")), new GeoPolygon(Box("5", "5", "6", "6")) });
            Assert.That(multi.Contains(P("5.5", "5.5")), Is.True);
            Assert.That(multi.Contains(P("3", "3")), Is.False, "parçalar arası deniz");
            Assert.That(multi.Bounds.MaxLatE6, Is.EqualTo(6_000_000));
        }

        [Test]
        public void Validator_RejectsBadRings()
        {
            Assert.That(Error(new GeoRing(new[] { P("0", "0"), P("2", "2"), P("2", "0"), P("0", "1") })), Is.EqualTo(GeographyErrorKeys.HalkaKendiniKesiyor), "asimetrik papyon");
            Assert.That(Error(new GeoRing(new[] { P("0", "0"), P("0", "0"), P("1", "1"), P("1", "0") })), Is.EqualTo(GeographyErrorKeys.HalkaArdisikTekrar));
            Assert.That(Error(new GeoRing(new[] { P("0", "0"), P("1", "1"), P("2", "2") })), Is.EqualTo(GeographyErrorKeys.HalkaAlanSifir));
            Assert.That(Error(new GeoRing(new[] { P("0", "179"), P("1", "-179"), P("1", "179") })), Is.EqualTo(GeographyErrorKeys.HalkaDikisGeciyor));
            Assert.That(Error(new GeoRing(new[] { P("0", "0"), P("0", "2"), P("0", "1"), P("1", "1") })), Is.EqualTo(GeographyErrorKeys.HalkaKendiniKesiyor), "sivri geri dönüş");
        }

        [Test]
        public void Validator_RejectsHoleOutsideOrTouchingOuter()
        {
            var outside = new GeoPolygon(Box("0", "0", "10", "10"), new[] { Box("20", "20", "21", "21") });
            var touching = new GeoPolygon(Box("0", "0", "10", "10"), new[] { Box("0", "4", "2", "6") });
            Assert.That(GeometryValidator.ValidatePolygon(outside).ErrorKey, Is.EqualTo(GeographyErrorKeys.DelikDisHalkaDisinda));
            Assert.That(GeometryValidator.ValidatePolygon(touching).ErrorKey, Is.EqualTo(GeographyErrorKeys.DelikDisHalkaDisinda));
        }

        [Test]
        public void Atlas_FindsRegion_LowestIdOnSharedBorder_NoneAtSea()
        {
            GeoWorld world = Build();
            Assert.That(world.Atlas.FindRegion(P("40.5", "28.5")), Is.EqualTo(A));
            Assert.That(world.Atlas.FindRegion(P("40.5", "29.5")), Is.EqualTo(B));
            Assert.That(world.Atlas.FindRegion(P("40.5", "29")), Is.EqualTo(A), "ortak sınır → küçük kimlik");
            Assert.That(world.Atlas.FindRegion(P("39.5", "28.5")), Is.EqualTo(RegionId.None), "deniz");
            Assert.That(world.AddRegion(A, Square("0", "0", "1", "1"), Red).ErrorKey, Is.EqualTo(GeographyErrorKeys.BolgeTekrar));
        }

        private static string Error(GeoRing ring) => GeometryValidator.ValidateRing(ring).ErrorKey;
    }
}
