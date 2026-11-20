using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content.Packages
{
    /// <summary>Paketteki bölge kaydı: değişmez geometri, başlangıç sahibi ve (ölçekli) nüfus.</summary>
    public sealed class PackageRegion
    {
        public PackageRegion(RegionId id, CountryId owner, string nameKey, long population, GeoMultiPolygon shape)
        {
            Id = id;
            Owner = owner;
            NameKey = nameKey;
            Population = population;
            Shape = shape;
        }

        public RegionId Id { get; }

        public CountryId Owner { get; }

        public string NameKey { get; }

        public long Population { get; }

        public GeoMultiPolygon Shape { get; }
    }
}
