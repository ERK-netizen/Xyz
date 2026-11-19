using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>Tüm kara birlikleri ve kuşatma altındaki şehirler.</summary>
    public sealed class WarfareState
    {
        public StableIdMap<LandUnitId, LandUnit> Units { get; } = new StableIdMap<LandUnitId, LandUnit>();

        public StableIdAllocator UnitIds { get; internal set; } = new StableIdAllocator();

        internal List<CityId> BesiegedList { get; } = new List<CityId>();

        /// <summary>Kuşatma altındaki şehirler (P14'te güncellenir), artan kimlik sırasında.</summary>
        public IReadOnlyList<CityId> Besieged => BesiegedList;

        public bool IsBesieged(CityId city) => BesiegedList.BinarySearch(city) >= 0;
    }
}
