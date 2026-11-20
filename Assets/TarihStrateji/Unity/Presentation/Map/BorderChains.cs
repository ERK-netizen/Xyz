using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Unity.Presentation.Map
{
    /// <summary>
    /// Sınır kenarlarını sürekli çizgilere (zincir) birleştirir (yalnız sunum): aynı türden (ülke sınırı ya
    /// da kıyı) kenarlar, derecesi 2 olan uç noktalardan geçilerek uç uca eklenir. Derecesi 2 olmayan noktalar
    /// (üçlü sınır, kıyı–sınır birleşimi) zincir ucudur; sadeleştirme uçları koruduğu için LOD'da kopukluk olmaz.
    /// Kapalı döngüler (ada kıyısı) ilk noktayla kapanır. Sonuç girdi sırasından türetilir, kararlıdır.
    /// </summary>
    public static class BorderChains
    {
        public readonly struct Chain
        {
            public Chain(List<GeoPoint> points, bool isCoast)
            {
                Points = points;
                IsCoast = isCoast;
            }

            public List<GeoPoint> Points { get; }

            public bool IsCoast { get; }
        }

        public static void Build(IReadOnlyList<CountryBorders.Segment> segments, List<Chain> into)
        {
            into.Clear();
            var incident = new Dictionary<(GeoPoint, bool), List<int>>();
            for (int i = 0; i < segments.Count; i++)
            {
                Attach(incident, (segments[i].A, segments[i].IsCoast), i);
                Attach(incident, (segments[i].B, segments[i].IsCoast), i);
            }

            var used = new bool[segments.Count];

            // Önce açık zincirler (derecesi 2 olmayan uçtan başlayanlar), sonra kalan kapalı döngüler.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < segments.Count; i++)
                {
                    if (used[i])
                    {
                        continue;
                    }

                    CountryBorders.Segment s = segments[i];
                    bool coast = s.IsCoast;
                    GeoPoint start;
                    if (pass == 0)
                    {
                        if (Degree(incident, s.A, coast) != 2)
                        {
                            start = s.A;
                        }
                        else if (Degree(incident, s.B, coast) != 2)
                        {
                            start = s.B;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        start = s.A;
                    }

                    into.Add(new Chain(Walk(segments, incident, used, i, start, coast), coast));
                }
            }
        }

        private static List<GeoPoint> Walk(IReadOnlyList<CountryBorders.Segment> segments, Dictionary<(GeoPoint, bool), List<int>> incident,
            bool[] used, int first, GeoPoint start, bool coast)
        {
            var points = new List<GeoPoint> { start };
            int current = first;
            GeoPoint at = start;
            while (current >= 0)
            {
                used[current] = true;
                CountryBorders.Segment s = segments[current];
                GeoPoint next = s.A == at ? s.B : s.A;
                points.Add(next);
                at = next;
                current = -1;
                if (Degree(incident, at, coast) == 2)
                {
                    List<int> around = incident[(at, coast)];
                    for (int k = 0; k < around.Count; k++)
                    {
                        if (!used[around[k]])
                        {
                            current = around[k];
                            break;
                        }
                    }
                }
            }

            return points;
        }

        private static int Degree(Dictionary<(GeoPoint, bool), List<int>> incident, GeoPoint p, bool coast) =>
            incident.TryGetValue((p, coast), out List<int> list) ? list.Count : 0;

        private static void Attach(Dictionary<(GeoPoint, bool), List<int>> incident, (GeoPoint, bool) key, int segment)
        {
            if (!incident.TryGetValue(key, out List<int> list))
            {
                list = new List<int>(2);
                incident.Add(key, list);
            }

            list.Add(segment);
        }
    }
}
