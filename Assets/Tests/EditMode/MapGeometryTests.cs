using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Map;
using UnityEngine;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Ülke haritası geometrisi (ADR-011): projeksiyon, üçgenleme ve yalnız ülke sınırlarının çıkarımı.</summary>
    public class MapGeometryTests
    {
        private static GeoPoint P(int lat, int lon) => new GeoPoint(lat * 1_000_000, lon * 1_000_000);

        [Test]
        public void Projection_RoundTripsToTheMicroDegree()
        {
            var istanbul = new GeoPoint(41_010_000, 28_970_000);
            Vector2 world = MapProjection.ToWorld(istanbul);
            Assert.That(world.y, Is.EqualTo(41.01f).Within(1e-4f));
            Assert.That(world.x, Is.EqualTo(28.97f * MapProjection.LonScale).Within(1e-4f));
            GeoPoint back = MapProjection.ToGeo(world);
            Assert.That(Mathf.Abs(back.LatitudeE6 - istanbul.LatitudeE6), Is.LessThanOrEqualTo(4));
            Assert.That(Mathf.Abs(back.LongitudeE6 - istanbul.LongitudeE6), Is.LessThanOrEqualTo(8));
        }

        [Test]
        public void Triangulator_CoversConcaveRingsInEitherWinding()
        {
            // L biçimli (içbükey) halka: alan 3 birim kare.
            var ring = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 1), new Vector2(1, 1), new Vector2(1, 2), new Vector2(0, 2) };
            foreach (bool reversed in new[] { false, true })
            {
                var r = new List<Vector2>(ring);
                if (reversed)
                {
                    r.Reverse();
                }

                var triangles = new List<int>();
                PolygonTriangulator.Triangulate(r, 0, triangles);
                Assert.That(triangles.Count, Is.EqualTo(12), "n − 2 üçgen");
                float area = 0f;
                for (int i = 0; i < triangles.Count; i += 3)
                {
                    area += Mathf.Abs(PolygonTriangulator.SignedArea(new[] { r[triangles[i]], r[triangles[i + 1]], r[triangles[i + 2]] }));
                }

                Assert.That(area, Is.EqualTo(3f).Within(1e-4f));
            }
        }

        [Test]
        public void Borders_HideInternalRegionEdges_AndKeepCountryBordersAndCoasts()
        {
            // Üç kare bölge yan yana: 1 ve 2 aynı ülke (A), 3 başka ülke (B).
            var a = new CountryId(1UL);
            var b = new CountryId(2UL);
            var regions = new List<RegionView>
            {
                new RegionView(new RegionId(1UL), a, a, 0),
                new RegionView(new RegionId(2UL), a, a, 0),
                new RegionView(new RegionId(3UL), b, a, 2),
            };
            var rings = new List<List<RegionRingView>>
            {
                Square(0, 0),
                Square(0, 1),
                Square(0, 2),
            };
            var segments = new List<CountryBorders.Segment>();
            CountryBorders.Compute(regions, rings, segments);

            int coast = segments.FindAll(s => s.IsCoast).Count;
            int border = segments.FindAll(s => !s.IsCoast).Count;
            Assert.That(border, Is.EqualTo(1), "yalnız A–B sınırı (sahibe göre; işgal sınırı taşımaz)");
            Assert.That(coast, Is.EqualTo(8), "dış çevre: 3 + 3 üst/alt + 2 yan");
            Assert.That(segments.Exists(s => !s.IsCoast && s.A.LongitudeE6 == 1_000_000 && s.B.LongitudeE6 == 1_000_000), Is.False, "A içindeki bölge sınırı çizilmez");
        }

        private static List<RegionRingView> Square(int lat, int lon) => new List<RegionRingView>
        {
            new RegionRingView(0, false, new[] { P(lat, lon), P(lat, lon + 1), P(lat + 1, lon + 1), P(lat + 1, lon) }),
        };
    }
}
