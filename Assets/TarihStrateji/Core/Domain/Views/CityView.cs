using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>Sunum için şehir görünümü: gerçek konum, gelişim seviyesi (1–10), sahip ≠ kontrolcü, sahibinin başkenti mi.</summary>
    public readonly struct CityView
    {
        public CityView(CityId id, GeoPoint location, string nameKey, int developmentLevel, CountryId owner, CountryId controller, bool isBesieged, bool isPort, bool isCapital = false)
        {
            Id = id;
            Location = location;
            NameKey = nameKey;
            DevelopmentLevel = developmentLevel;
            Owner = owner;
            Controller = controller;
            IsBesieged = isBesieged;
            IsPort = isPort;
            IsCapital = isCapital;
        }

        public CityId Id { get; }

        public GeoPoint Location { get; }

        public string NameKey { get; }

        public int DevelopmentLevel { get; }

        public CountryId Owner { get; }

        public CountryId Controller { get; }

        public bool IsBesieged { get; }

        public bool IsPort { get; }

        /// <summary>Şehir, hukukî sahibinin senaryo başkentidir.</summary>
        public bool IsCapital { get; }
    }
}
