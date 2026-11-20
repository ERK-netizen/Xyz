using System;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Bölge geometrisinin tek halkası (salt okunur kopya): <see cref="Polygon"/> bölgenin kaçıncı
    /// çokgeni (ada) olduğu, <see cref="IsHole"/> deliği belirtir. Noktalar kapanış tekrarı olmadan
    /// verilir. Sunum ülke sınırını, komşu halkası başka sahibe ait (ya da hiç olmayan) kenarlardan çizer.
    /// </summary>
    public readonly struct RegionRingView
    {
        public RegionRingView(int polygon, bool isHole, GeoPoint[] points)
        {
            Polygon = polygon;
            IsHole = isHole;
            Points = points ?? throw new ArgumentNullException(nameof(points));
        }

        public int Polygon { get; }

        public bool IsHole { get; }

        public GeoPoint[] Points { get; }
    }
}
