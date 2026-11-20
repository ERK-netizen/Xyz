using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>Haritada dokunulan şey: birlik, şehir, bölge veya yalnız nokta (yalnız sunum).</summary>
    public readonly struct MapPick
    {
        public static readonly MapPick None = default;

        private MapPick(MapPickKind kind, ulong unit, bool unitIsOwnLand, CityId city, RegionId region, GeoPoint point)
        {
            Kind = kind;
            Unit = unit;
            UnitIsOwnLand = unitIsOwnLand;
            City = city;
            Region = region;
            Point = point;
        }

        public MapPickKind Kind { get; }

        public ulong Unit { get; }

        /// <summary>Oyuncunun kara birliği mi (seçilip emir verilebilir).</summary>
        public bool UnitIsOwnLand { get; }

        public CityId City { get; }

        public RegionId Region { get; }

        public GeoPoint Point { get; }

        public static MapPick OfUnit(ulong unit, bool ownLand, RegionId region, GeoPoint at) => new MapPick(MapPickKind.Unit, unit, ownLand, CityId.None, region, at);

        public static MapPick OfCity(CityId city, RegionId region, GeoPoint at) => new MapPick(MapPickKind.City, 0UL, false, city, region, at);

        public static MapPick OfRegion(RegionId region, GeoPoint at) => new MapPick(MapPickKind.Region, 0UL, false, CityId.None, region, at);

        public static MapPick OfPoint(GeoPoint at) => new MapPick(MapPickKind.Point, 0UL, false, CityId.None, RegionId.None, at);

        /// <summary>Emir hedefi: birlik → birlik, şehir → şehir, aksi hâlde nokta.</summary>
        public TargetRef ToTarget()
        {
            switch (Kind)
            {
                case MapPickKind.Unit:
                    return TargetRef.Entity(TargetKind.LandUnit, Unit);
                case MapPickKind.City:
                    return TargetRef.Entity(TargetKind.City, City.Value);
                case MapPickKind.None:
                    return TargetRef.None;
                default:
                    return TargetRef.AtPoint(Point);
            }
        }
    }
}
