using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content.Packages
{
    /// <summary>Paketteki şehir kaydı: konum, bölge, ulaşım düğümü, kademe (nüfus/gelişim), liman, 1914 sahibi.</summary>
    public sealed class PackageCity
    {
        public PackageCity(CityId id, string nameKey, GeoPoint location, RegionId region, TransportNodeId node, int tier, bool isPort, CountryId owner)
        {
            Id = id;
            NameKey = nameKey;
            Location = location;
            Region = region;
            Node = node;
            Tier = tier;
            IsPort = isPort;
            Owner = owner;
        }

        public CityId Id { get; }

        public string NameKey { get; }

        public GeoPoint Location { get; }

        public RegionId Region { get; }

        public TransportNodeId Node { get; }

        /// <summary>Kademe 1–5: gelişim seviyesi ve ekonomi yerleşimi buradan türetilir.</summary>
        public int Tier { get; }

        public bool IsPort { get; }

        public CountryId Owner { get; }
    }
}
