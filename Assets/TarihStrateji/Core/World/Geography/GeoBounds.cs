using System;
using TarihStrateji.Core.Domain.Geography;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Enlem/boylam sınır kutusu (E6). Halkalar ±180° dikişini geçmediği için (içerik derleyici
    /// keser) kutu her zaman MinLon ≤ MaxLon biçimindedir.
    /// </summary>
    [Serializable]
    public readonly struct GeoBounds
    {
        public GeoBounds(int minLatE6, int minLonE6, int maxLatE6, int maxLonE6)
        {
            MinLatE6 = minLatE6;
            MinLonE6 = minLonE6;
            MaxLatE6 = maxLatE6;
            MaxLonE6 = maxLonE6;
        }

        public int MinLatE6 { get; }

        public int MinLonE6 { get; }

        public int MaxLatE6 { get; }

        public int MaxLonE6 { get; }

        public bool Contains(GeoPoint p) =>
            p.LatitudeE6 >= MinLatE6 && p.LatitudeE6 <= MaxLatE6 && p.LongitudeE6 >= MinLonE6 && p.LongitudeE6 <= MaxLonE6;

        public bool Intersects(GeoBounds other) =>
            MinLatE6 <= other.MaxLatE6 && other.MinLatE6 <= MaxLatE6 && MinLonE6 <= other.MaxLonE6 && other.MinLonE6 <= MaxLonE6;

        public GeoBounds Union(GeoBounds other) => new GeoBounds(
            Math.Min(MinLatE6, other.MinLatE6), Math.Min(MinLonE6, other.MinLonE6),
            Math.Max(MaxLatE6, other.MaxLatE6), Math.Max(MaxLonE6, other.MaxLonE6));
    }
}
