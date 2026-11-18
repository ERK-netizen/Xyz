using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.World.Geo
{
    /// <summary>Coğrafi sabitler.</summary>
    public static class GeoConstants
    {
        /// <summary>Yerin ortalama yarıçapı (IUGG), km.</summary>
        public static readonly Fixed64 EarthRadiusKm = Fixed64.Parse("6371.0088");

        /// <summary>Merkez açısını (radyan) km'ye çevirir.</summary>
        public static Fixed64 ArcKm(Fixed64 centralAngleRadians)
        {
            return centralAngleRadians * EarthRadiusKm;
        }

        /// <summary>İki coğrafi nokta arası büyük çember mesafesi (km); ±180° dikişini bilir.</summary>
        public static Fixed64 GreatCircleKm(Fixed64 latA, Fixed64 lonA, Fixed64 latB, Fixed64 lonB)
        {
            return ArcKm(FixedTrig.CentralAngleRadians(latA, lonA, latB, lonB));
        }
    }
}
