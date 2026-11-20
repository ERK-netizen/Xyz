using System.Collections.Generic;
using NUnit.Framework;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Unity.Presentation.Map;
using UnityEngine;

namespace TarihStrateji.Tests.EditMode
{
    /// <summary>Şehir adı yerleşimi (Adım 15): öncelik, çakışmasızlık, ekran sınırı, kararlılık ve öncelik/genişlik kuralları.</summary>
    public class MapLabelTests
    {
        private static LabelPlacer.Candidate C(ulong id, float x, float y, int priority, float width = 80f) =>
            new LabelPlacer.Candidate(id, new Vector2(x, y), width, 30f, 10f, priority);

        private static List<LabelPlacer.Placement> Place(List<LabelPlacer.Candidate> candidates, float w = 1000f, float h = 1000f)
        {
            var placed = new List<LabelPlacer.Placement>();
            new LabelPlacer().Place(candidates, w, h, placed);
            return placed;
        }

        [Test]
        public void HigherPriority_TakesPreferredSide_LowerOneMovesAside()
        {
            List<LabelPlacer.Placement> placed = Place(new List<LabelPlacer.Candidate> { C(1, 100f, 100f, 5), C(2, 110f, 100f, 9), C(3, 500f, 500f, 1) });
            Assert.That(placed.Count, Is.EqualTo(3));
            Assert.That(placed[0].Id, Is.EqualTo(2UL), "öncelik sırası");
            Assert.That(placed[0].Left, Is.EqualTo(110f + 10f + LabelPlacer.Gap), "öncelikli etiket sağda");
            Assert.That(placed[1].Id, Is.EqualTo(1UL));
            Assert.That(placed[1].Left + placed[1].Width, Is.LessThanOrEqualTo(100f - 10f), "ikinci etiket sola kayar");
        }

        [Test]
        public void PlacedLabels_NeverOverlapEachOtherOrForeignMarkers()
        {
            var candidates = new List<LabelPlacer.Candidate>();
            ulong id = 1;
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    candidates.Add(C(id, 40f + (x * 70f), 40f + (y * 70f), (int)(id % 7), 60f + ((id * 13) % 50)));
                    id++;
                }
            }

            List<LabelPlacer.Placement> placed = Place(candidates);
            Assert.That(placed.Count, Is.GreaterThan(0).And.LessThan(candidates.Count), "yoğun kümede bazı adlar gizlenir");
            for (int i = 0; i < placed.Count; i++)
            {
                LabelPlacer.Placement a = placed[i];
                Assert.That(a.Left >= 0f && a.Top >= 0f && a.Left + a.Width <= 1000f && a.Top + a.Height <= 1000f, Is.True, "ekran içinde");
                for (int j = i + 1; j < placed.Count; j++)
                {
                    Assert.That(a.Overlaps(placed[j].Left, placed[j].Top, placed[j].Width, placed[j].Height), Is.False, "etiketler çakışmaz");
                }

                for (int k = 0; k < candidates.Count; k++)
                {
                    LabelPlacer.Candidate c = candidates[k];
                    if (c.Id != a.Id)
                    {
                        Assert.That(a.Overlaps(c.Anchor.x - 10f, c.Anchor.y - 10f, 20f, 20f), Is.False, "başka şehir işaretini örtmez");
                    }
                }
            }
        }

        [Test]
        public void LabelThatFitsNowhereOnScreen_IsHidden()
        {
            List<LabelPlacer.Placement> placed = Place(new List<LabelPlacer.Candidate> { C(1, 990f, 10f, 1) });
            Assert.That(placed, Is.Empty);
            placed = Place(new List<LabelPlacer.Candidate> { C(1, 990f, 500f, 1) });
            Assert.That(placed.Count, Is.EqualTo(1));
            Assert.That(placed[0].Left + placed[0].Width, Is.LessThanOrEqualTo(990f - 10f), "sağ kenarda sola geçer");
        }

        [Test]
        public void Placement_IsStable_RegardlessOfInputOrder_AndReusable()
        {
            var forward = new List<LabelPlacer.Candidate> { C(1, 100f, 100f, 3), C(2, 120f, 100f, 3), C(3, 140f, 110f, 3), C(4, 300f, 300f, 2) };
            var backward = new List<LabelPlacer.Candidate>(forward);
            backward.Reverse();
            var placer = new LabelPlacer();
            var a = new List<LabelPlacer.Placement>();
            var b = new List<LabelPlacer.Placement>();
            placer.Place(forward, 1000f, 1000f, a);
            placer.Place(backward, 1000f, 1000f, b);
            Assert.That(b.Count, Is.EqualTo(a.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(b[i].Id == a[i].Id && b[i].Left == a[i].Left && b[i].Top == a[i].Top, Is.True, "eşit öncelikte küçük kimlik önce; yeniden kullanım aynı sonucu verir");
            }
        }

        [Test]
        public void Priority_AndWidthEstimate()
        {
            var big = new CityView(new CityId(1), new GeoPoint(0, 0), "a", 7, new CountryId(1), new CountryId(1), false, false);
            var small = new CityView(new CityId(2), new GeoPoint(0, 0), "b", 6, new CountryId(1), new CountryId(1), false, false);
            Assert.That(MapLabelLayer.Priority(big, false), Is.GreaterThan(MapLabelLayer.Priority(small, true)), "gelişim baskın");
            Assert.That(MapLabelLayer.Priority(small, true), Is.GreaterThan(MapLabelLayer.Priority(small, false)), "eşitlikte kendi şehri");
            Assert.That(MapLabelLayer.EstimateWidth("İstanbul", 26), Is.GreaterThan(MapLabelLayer.EstimateWidth("Van", 26)));
        }
    }
}
