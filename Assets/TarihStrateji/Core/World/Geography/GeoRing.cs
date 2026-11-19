using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Kapalı halka: köşeler E6 tamsayı, son köşe ilkine bağlanır (kapanış tekrar edilmez). Düzlemde
    /// x = boylam, y = enlem olarak işlenir; halka ±180° dikişini geçmez (doğrulayıcı reddeder).
    /// Nokta konumu tam tamsayı çapraz çarpımla hesaplanır: |Δ| ≤ 3,6·10⁸ olduğundan çarpımlar
    /// 64 bite sığar. Değişmez (immutable) içerik verisidir; kayıtlara yazılmaz.
    /// </summary>
    public sealed class GeoRing
    {
        private readonly GeoPoint[] _vertices;

        public GeoRing(IReadOnlyList<GeoPoint> vertices)
        {
            if (vertices == null || vertices.Count < 3)
            {
                throw new ArgumentException(GeographyErrorKeys.HalkaKoseYetersiz, nameof(vertices));
            }

            int count = vertices.Count;
            if (vertices[0] == vertices[count - 1])
            {
                count--;
            }

            _vertices = new GeoPoint[count];
            int minLat = int.MaxValue;
            int minLon = int.MaxValue;
            int maxLat = int.MinValue;
            int maxLon = int.MinValue;
            for (int i = 0; i < count; i++)
            {
                GeoPoint p = vertices[i];
                _vertices[i] = p;
                minLat = Math.Min(minLat, p.LatitudeE6);
                maxLat = Math.Max(maxLat, p.LatitudeE6);
                minLon = Math.Min(minLon, p.LongitudeE6);
                maxLon = Math.Max(maxLon, p.LongitudeE6);
            }

            Bounds = new GeoBounds(minLat, minLon, maxLat, maxLon);
        }

        public int Count => _vertices.Length;

        public GeoBounds Bounds { get; }

        public GeoPoint this[int index] => _vertices[index];

        public PointLocation Locate(GeoPoint p)
        {
            if (!Bounds.Contains(p))
            {
                return PointLocation.Outside;
            }

            long px = p.LongitudeE6;
            long py = p.LatitudeE6;
            bool inside = false;
            int n = _vertices.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                long ax = _vertices[j].LongitudeE6;
                long ay = _vertices[j].LatitudeE6;
                long bx = _vertices[i].LongitudeE6;
                long by = _vertices[i].LatitudeE6;

                long cross = ((bx - ax) * (py - ay)) - ((px - ax) * (by - ay));
                if (cross == 0L && px >= Math.Min(ax, bx) && px <= Math.Max(ax, bx) && py >= Math.Min(ay, by) && py <= Math.Max(ay, by))
                {
                    return PointLocation.OnBoundary;
                }

                if ((ay > py) != (by > py))
                {
                    // Kenar yukarı gidiyorsa nokta solda (cross > 0) iken ışın kenarı keser.
                    if ((by > ay) ? cross > 0L : cross < 0L)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside ? PointLocation.Inside : PointLocation.Outside;
        }
    }
}
