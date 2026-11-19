using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Geography
{
    /// <summary>
    /// Coğrafi nokta: enlem ve boylam mikro-derece (E6) tamsayı. Kural kodunda kayan nokta yoktur;
    /// E6 değeri, ölçeği 10⁶ olan <c>Fixed64</c> ham değeriyle birebir aynıdır (derece). Boylam
    /// (−180°, 180°] aralığına normalize edilir; ±180° aynı meridyendir (CLAUDE.md §2.3).
    /// </summary>
    [Serializable]
    public readonly struct GeoPoint : IEquatable<GeoPoint>
    {
        public const int MaxLatitudeE6 = 90_000_000;
        public const int HalfTurnE6 = 180_000_000;
        public const long FullTurnE6 = 360_000_000L;

        public GeoPoint(int latitudeE6, int longitudeE6)
        {
            if (latitudeE6 < -MaxLatitudeE6 || latitudeE6 > MaxLatitudeE6)
            {
                throw new ArgumentOutOfRangeException(nameof(latitudeE6), "Enlem [-90, 90] dışında.");
            }

            LatitudeE6 = latitudeE6;
            LongitudeE6 = NormalizeLongitude(longitudeE6);
        }

        public int LatitudeE6 { get; }

        public int LongitudeE6 { get; }

        /// <summary>Boylamı (−180°, 180°] aralığına getirir.</summary>
        public static int NormalizeLongitude(long longitudeE6)
        {
            long r = longitudeE6 % FullTurnE6;
            if (r <= -HalfTurnE6)
            {
                r += FullTurnE6;
            }
            else if (r > HalfTurnE6)
            {
                r -= FullTurnE6;
            }

            return (int)r;
        }

        /// <summary>a'dan b'ye en kısa yönde boylam farkı, (−180°, 180°].</summary>
        public static int LongitudeDelta(int fromE6, int toE6) => NormalizeLongitude((long)toE6 - fromE6);

        public bool Equals(GeoPoint other) => LatitudeE6 == other.LatitudeE6 && LongitudeE6 == other.LongitudeE6;

        public override bool Equals(object obj) => obj is GeoPoint other && Equals(other);

        public override int GetHashCode() => (LatitudeE6 * 397) ^ LongitudeE6;

        public static bool operator ==(GeoPoint a, GeoPoint b) => a.Equals(b);

        public static bool operator !=(GeoPoint a, GeoPoint b) => !a.Equals(b);

        public override string ToString() =>
            "(" + LatitudeE6.ToString(CultureInfo.InvariantCulture) + "e-6, " + LongitudeE6.ToString(CultureInfo.InvariantCulture) + "e-6)";
    }
}
