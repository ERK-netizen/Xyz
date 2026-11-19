using System;
using System.Numerics;
using TarihStrateji.Core.Domain.Commands;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Geometri kalite kuralları (§22.5, MAP-G01): halka en az üç ayrı köşe, ardışık tekrar yok,
    /// ±180° dikişini geçmez (ardışık boylam farkı &lt; 180°), alanı sıfır değil, kendini kesmez;
    /// delikler dış halkanın içinde ve birbirine/dış halkaya değmez. Tam tamsayı aritmetiği
    /// kullanılır. Kesişim denetimi O(n²) olduğundan içerik yüklenirken ve derleyicide çalışır;
    /// günlük fazlarda çağrılmaz.
    /// </summary>
    public static class GeometryValidator
    {
        private const long MaxEdgeLongitudeSpanE6 = 180_000_000L;

        public static ValidationResult ValidateRing(GeoRing ring)
        {
            int n = ring.Count;
            if (n < 3)
            {
                return ValidationResult.Fail(GeographyErrorKeys.HalkaKoseYetersiz);
            }

            for (int i = 0; i < n; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % n];
                if (a == b)
                {
                    return ValidationResult.Fail(GeographyErrorKeys.HalkaArdisikTekrar);
                }

                if (Math.Abs((long)b.LongitudeE6 - a.LongitudeE6) >= MaxEdgeLongitudeSpanE6)
                {
                    return ValidationResult.Fail(GeographyErrorKeys.HalkaDikisGeciyor);
                }
            }

            if (SignedDoubleArea(ring).IsZero)
            {
                return ValidationResult.Fail(GeographyErrorKeys.HalkaAlanSifir);
            }

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    bool adjacent = j == i + 1 || (i == 0 && j == n - 1);
                    if (adjacent)
                    {
                        if (CollinearOverlap(ring, i, j))
                        {
                            return ValidationResult.Fail(GeographyErrorKeys.HalkaKendiniKesiyor);
                        }

                        continue;
                    }

                    if (SegmentsTouch(ring, i, ring, j))
                    {
                        return ValidationResult.Fail(GeographyErrorKeys.HalkaKendiniKesiyor);
                    }
                }
            }

            return ValidationResult.Ok;
        }

        public static ValidationResult ValidatePolygon(GeoPolygon polygon)
        {
            ValidationResult outer = ValidateRing(polygon.Outer);
            if (!outer.IsValid)
            {
                return outer;
            }

            for (int h = 0; h < polygon.HoleCount; h++)
            {
                GeoRing hole = polygon.Hole(h);
                ValidationResult valid = ValidateRing(hole);
                if (!valid.IsValid)
                {
                    return valid;
                }

                if (polygon.Outer.Locate(hole[0]) != PointLocation.Inside || RingsTouch(hole, polygon.Outer))
                {
                    return ValidationResult.Fail(GeographyErrorKeys.DelikDisHalkaDisinda);
                }

                for (int k = 0; k < h; k++)
                {
                    GeoRing other = polygon.Hole(k);
                    if (RingsTouch(hole, other) || other.Locate(hole[0]) != PointLocation.Outside || hole.Locate(other[0]) != PointLocation.Outside)
                    {
                        return ValidationResult.Fail(GeographyErrorKeys.DelikDisHalkaDisinda);
                    }
                }
            }

            return ValidationResult.Ok;
        }

        public static ValidationResult ValidateMultiPolygon(GeoMultiPolygon multi)
        {
            for (int i = 0; i < multi.Count; i++)
            {
                ValidationResult valid = ValidatePolygon(multi[i]);
                if (!valid.IsValid)
                {
                    return valid;
                }
            }

            return ValidationResult.Ok;
        }

        /// <summary>Shoelace iki katı alanı (E6²). İşaret yönü verir: pozitif = saat yönünün tersi.</summary>
        public static BigInteger SignedDoubleArea(GeoRing ring)
        {
            BigInteger sum = BigInteger.Zero;
            int n = ring.Count;
            for (int i = 0; i < n; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % n];
                sum += ((long)a.LongitudeE6 * b.LatitudeE6) - ((long)b.LongitudeE6 * a.LatitudeE6);
            }

            return sum;
        }

        private static bool RingsTouch(GeoRing a, GeoRing b)
        {
            if (!a.Bounds.Intersects(b.Bounds))
            {
                return false;
            }

            for (int i = 0; i < a.Count; i++)
            {
                for (int j = 0; j < b.Count; j++)
                {
                    if (SegmentsTouch(a, i, b, j))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SegmentsTouch(GeoRing r1, int i, GeoRing r2, int j)
        {
            var p1 = r1[i];
            var p2 = r1[(i + 1) % r1.Count];
            var q1 = r2[j];
            var q2 = r2[(j + 1) % r2.Count];

            int o1 = Orientation(p1.LongitudeE6, p1.LatitudeE6, p2.LongitudeE6, p2.LatitudeE6, q1.LongitudeE6, q1.LatitudeE6);
            int o2 = Orientation(p1.LongitudeE6, p1.LatitudeE6, p2.LongitudeE6, p2.LatitudeE6, q2.LongitudeE6, q2.LatitudeE6);
            int o3 = Orientation(q1.LongitudeE6, q1.LatitudeE6, q2.LongitudeE6, q2.LatitudeE6, p1.LongitudeE6, p1.LatitudeE6);
            int o4 = Orientation(q1.LongitudeE6, q1.LatitudeE6, q2.LongitudeE6, q2.LatitudeE6, p2.LongitudeE6, p2.LatitudeE6);

            if (o1 != o2 && o3 != o4 && o1 != 0 && o2 != 0 && o3 != 0 && o4 != 0)
            {
                return true;
            }

            return (o1 == 0 && OnSegment(p1, p2, q1)) || (o2 == 0 && OnSegment(p1, p2, q2))
                || (o3 == 0 && OnSegment(q1, q2, p1)) || (o4 == 0 && OnSegment(q1, q2, p2));
        }

        /// <summary>Ardışık iki kenar ortak köşede geri dönüyorsa (sivri uç, sıfır genişlik) kesişim sayılır.</summary>
        private static bool CollinearOverlap(GeoRing ring, int i, int j)
        {
            int n = ring.Count;
            int first = j == i + 1 ? i : j;
            var a = ring[first];
            var shared = ring[(first + 1) % n];
            var c = ring[(first + 2) % n];
            if (Orientation(a.LongitudeE6, a.LatitudeE6, shared.LongitudeE6, shared.LatitudeE6, c.LongitudeE6, c.LatitudeE6) != 0)
            {
                return false;
            }

            long dx1 = (long)a.LongitudeE6 - shared.LongitudeE6;
            long dy1 = (long)a.LatitudeE6 - shared.LatitudeE6;
            long dx2 = (long)c.LongitudeE6 - shared.LongitudeE6;
            long dy2 = (long)c.LatitudeE6 - shared.LatitudeE6;
            return (dx1 * dx2) + (dy1 * dy2) > 0L;
        }

        private static int Orientation(long ax, long ay, long bx, long by, long cx, long cy)
        {
            long cross = ((bx - ax) * (cy - ay)) - ((by - ay) * (cx - ax));
            return cross > 0L ? 1 : cross < 0L ? -1 : 0;
        }

        private static bool OnSegment(TarihStrateji.Core.Domain.Geography.GeoPoint a, TarihStrateji.Core.Domain.Geography.GeoPoint b, TarihStrateji.Core.Domain.Geography.GeoPoint p) =>
            p.LongitudeE6 >= Math.Min(a.LongitudeE6, b.LongitudeE6) && p.LongitudeE6 <= Math.Max(a.LongitudeE6, b.LongitudeE6)
            && p.LatitudeE6 >= Math.Min(a.LatitudeE6, b.LatitudeE6) && p.LatitudeE6 <= Math.Max(a.LatitudeE6, b.LatitudeE6);
    }
}
