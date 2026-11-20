using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Ülke sınırı çıkarımı (yalnız sunum): bölge halkalarının kenarları uç noktalarıyla eşlenir. Aynı
    /// kenarı paylaşan iki bölgenin sahibi aynıysa kenar iç sınırdır ve çizilmez; sahipleri farklıysa ülke
    /// sınırı, kenarı tek bölge kullanıyorsa kıyı/dış sınırdır. Böylece haritada yalnız ülke sınırları
    /// görünür, idari bölgeler görünmez (kullanıcı kararı, ADR-011).
    /// </summary>
    public static class CountryBorders
    {
        public readonly struct Segment
        {
            public Segment(GeoPoint a, GeoPoint b, bool isCoast)
            {
                A = a;
                B = b;
                IsCoast = isCoast;
            }

            public GeoPoint A { get; }

            public GeoPoint B { get; }

            /// <summary>Komşu bölgesi olmayan kenar (kıyı veya haritanın dış sınırı).</summary>
            public bool IsCoast { get; }
        }

        /// <summary><paramref name="rings"/>: bölge kimliği sırasıyla her bölgenin halkaları; sahip <paramref name="regions"/>'tan.</summary>
        public static void Compute(IReadOnlyList<RegionView> regions, IReadOnlyList<List<RegionRingView>> rings, List<Segment> into)
        {
            into.Clear();
            var owners = new Dictionary<(long, long), List<CountryId>>();
            var order = new List<(GeoPoint, GeoPoint, (long, long))>();
            for (int r = 0; r < regions.Count; r++)
            {
                List<RegionRingView> regionRings = rings[r];
                for (int k = 0; k < regionRings.Count; k++)
                {
                    GeoPoint[] points = regionRings[k].Points;
                    for (int i = 0; i < points.Length; i++)
                    {
                        GeoPoint a = points[i];
                        GeoPoint b = points[(i + 1) % points.Length];
                        (long, long) key = Key(a, b);
                        if (!owners.TryGetValue(key, out List<CountryId> list))
                        {
                            list = new List<CountryId>(2);
                            owners.Add(key, list);
                            order.Add((a, b, key));
                        }

                        list.Add(regions[r].Owner);
                    }
                }
            }

            for (int i = 0; i < order.Count; i++)
            {
                List<CountryId> list = owners[order[i].Item3];
                if (list.Count == 1)
                {
                    into.Add(new Segment(order[i].Item1, order[i].Item2, true));
                }
                else if (list[0] != list[1])
                {
                    into.Add(new Segment(order[i].Item1, order[i].Item2, false));
                }
            }
        }

        private static (long, long) Key(GeoPoint a, GeoPoint b)
        {
            long ka = Pack(a);
            long kb = Pack(b);
            return ka < kb ? (ka, kb) : (kb, ka);
        }

        private static long Pack(GeoPoint p) => ((long)p.LatitudeE6 << 32) ^ (uint)p.LongitudeE6;
    }
}
