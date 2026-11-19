using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Deterministik coğrafi mesafe. Büyük çember (kiriş yöntemi, <see cref="FixedTrig"/>, iç
    /// hassasiyet 10⁻⁹) ile metre. E6 koordinat, ölçeği 10⁶ olan <see cref="Fixed64"/> derece ham
    /// değeriyle aynıdır. Sonuç tüm platformlarda birebir aynıdır (kayan nokta yok).
    /// </summary>
    public static class GeoMath
    {
        /// <summary>Yerin ortalama yarıçapı (IUGG), metre.</summary>
        public const long EarthRadiusMeters = 6_371_009L;

        public static long DistanceMeters(GeoPoint a, GeoPoint b)
        {
            if (a == b)
            {
                return 0L;
            }

            Fixed64 angle = FixedTrig.CentralAngleRadians(
                Fixed64.FromRaw(a.LatitudeE6), Fixed64.FromRaw(a.LongitudeE6),
                Fixed64.FromRaw(b.LatitudeE6), Fixed64.FromRaw(b.LongitudeE6));

            // angle.Raw = radyan × 10⁶; metre = radyan × R. Yarıda sıfırdan uzağa yuvarlama.
            long product = checked(angle.Raw * EarthRadiusMeters);
            return (product + (Fixed64.Scale / 2L)) / Fixed64.Scale;
        }
    }
}
