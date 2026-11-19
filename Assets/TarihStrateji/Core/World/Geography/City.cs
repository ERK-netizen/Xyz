using System;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Tekil şehir (§4.1, ADR-004 geçersiz): gerçek koordinat, bağlı bölge ve ulaşım düğümü, gelişim
    /// seviyesi 1–10, hukukî sahip ve fiilî kontrolcü ayrı, kuşatma durumu ve tahkimat. Sektör veya
    /// hücre yoktur; yapı kapasitesi gelişim seviyesinden türetilir. Durum değişikliğini yalnız
    /// SimulationKernel yapar.
    /// </summary>
    public sealed class City
    {
        public const int MinDevelopment = 1;
        public const int MaxDevelopment = 10;

        private int _developmentLevel;

        public City(CityId id, string nameKey, GeoPoint location, RegionId region, TransportNodeId node,
            int developmentLevel, CountryId owner, Fixed64 fortification)
        {
            Id = id;
            NameKey = nameKey ?? throw new ArgumentNullException(nameof(nameKey));
            Location = location;
            Region = region;
            Node = node;
            DevelopmentLevel = developmentLevel;
            Owner = owner;
            Controller = owner;
            Fortification = fortification;
        }

        public CityId Id { get; }

        public string NameKey { get; }

        public GeoPoint Location { get; }

        public RegionId Region { get; }

        public TransportNodeId Node { get; }

        public int DevelopmentLevel
        {
            get => _developmentLevel;
            set
            {
                if (value < MinDevelopment || value > MaxDevelopment)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), GeographyErrorKeys.SehirGelisimGecersiz);
                }

                _developmentLevel = value;
            }
        }

        /// <summary>Hukukî sahip: yalnız barış/antlaşma değiştirir.</summary>
        public CountryId Owner { get; set; }

        /// <summary>Fiilî kontrolcü: garnizon teslimi/işgalle değişir; sahipliği değiştirmez (CITY-005).</summary>
        public CountryId Controller { get; set; }

        public SiegeState Siege { get; set; }

        /// <summary>Tahkimat çarpanı (1 = tahkimatsız).</summary>
        public Fixed64 Fortification { get; set; }

        public bool IsOccupied => Controller != Owner;

        /// <summary>Yapı kapasitesi = seviye × seviye başına slot (denge verisi, §4.2). Altyapı slot tüketmez.</summary>
        public int BuildingSlots(int slotsPerLevel) => checked(_developmentLevel * slotsPerLevel);
    }
}
