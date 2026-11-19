using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>
    /// Hava kanadı: bir şehirde üslenir, görev hedefi üssten menzil içindeki bir hücredir. Kural
    /// mantığı <c>AirRules</c>, <c>AirCombatRules</c> ve <c>JointSupportRules</c>'tadır.
    /// </summary>
    public sealed class AirWing
    {
        internal AirWing(AirWingId id, CountryId owner, UnitTypeId type, CityId baseCity, RegionId homeRegion)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Base = baseCity;
            HomeRegion = homeRegion;
        }

        public AirWingId Id { get; }

        public CountryId Owner { get; }

        public UnitTypeId Type { get; }

        public CityId Base { get; internal set; }

        public RegionId HomeRegion { get; }

        public long Manpower { get; internal set; }

        public Fixed64 Equipment { get; internal set; }

        public Fixed64 Organization { get; internal set; }

        public AirMission Mission { get; internal set; }

        public HexId MissionTarget { get; internal set; }

        /// <summary>Coğrafi görev hedefi (birlik, şehir, filo veya nokta; ADR-011).</summary>
        public TargetRef GeoMissionTarget { get; internal set; }

        public Fixed64 Strength(AirWingTypeDefinition type)
        {
            return Fixed64.Min(Fixed64.FromRatio(Manpower, type.Crew), Equipment);
        }
    }
}
