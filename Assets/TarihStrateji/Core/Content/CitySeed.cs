using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Senaryo şehri: anahtar, konum (yüzde bir derece), ülke, kademe (nüfus/gelişim) ve liman.</summary>
    public readonly struct CitySeed
    {
        public CitySeed(string key, int latitude, int longitude, CountryId country, int tier, bool isPort)
        {
            Key = key;
            Latitude = latitude;
            Longitude = longitude;
            Country = country;
            Tier = tier;
            IsPort = isPort;
        }

        public string Key { get; }

        /// <summary>Enlem, yüzde bir derece (4101 = 41,01° K).</summary>
        public int Latitude { get; }

        /// <summary>Boylam, yüzde bir derece (2897 = 28,97° D).</summary>
        public int Longitude { get; }

        public CountryId Country { get; }

        public int Tier { get; }

        public bool IsPort { get; }
    }
}
