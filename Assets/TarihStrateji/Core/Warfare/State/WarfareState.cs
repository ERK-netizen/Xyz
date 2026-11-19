using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>Tüm kara birlikleri, filolar, hava kanatları; kuşatma ve abluka altındaki şehirler.</summary>
    public sealed class WarfareState
    {
        public StableIdMap<LandUnitId, LandUnit> Units { get; } = new StableIdMap<LandUnitId, LandUnit>();

        public StableIdAllocator UnitIds { get; internal set; } = new StableIdAllocator();

        public StableIdMap<FleetId, Fleet> Fleets { get; } = new StableIdMap<FleetId, Fleet>();

        public StableIdAllocator FleetIds { get; internal set; } = new StableIdAllocator();

        public StableIdMap<AirWingId, AirWing> AirWings { get; } = new StableIdMap<AirWingId, AirWing>();

        public StableIdAllocator AirWingIds { get; internal set; } = new StableIdAllocator();

        internal List<CityId> BesiegedList { get; } = new List<CityId>();

        internal List<CityId> BlockadedList { get; } = new List<CityId>();

        /// <summary>Ablukadaki liman şehirleri (P14'te güncellenir), artan kimlik sırasında.</summary>
        public IReadOnlyList<CityId> Blockaded => BlockadedList;

        public bool IsBlockaded(CityId city) => BlockadedList.BinarySearch(city) >= 0;

        /// <summary>Kuşatma altındaki şehirler (P14'te güncellenir), artan kimlik sırasında.</summary>
        public IReadOnlyList<CityId> Besieged => BesiegedList;

        public bool IsBesieged(CityId city) => BesiegedList.BinarySearch(city) >= 0;
    }
}
