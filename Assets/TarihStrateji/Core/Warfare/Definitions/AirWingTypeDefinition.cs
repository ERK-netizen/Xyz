using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>
    /// Hava kanadı türü. <see cref="AirAttack"/> hava üstünlüğü muharebesinde, <see cref="GroundAttack"/>
    /// kara ve deniz hedeflerine verilen ortak destekte kullanılır. Görev hedefi üssten büyük çember
    /// mesafesiyle <see cref="RangeKm"/> içinde olmalıdır; menzil dışı destek doğrulamada reddedilir.
    /// </summary>
    public sealed class AirWingTypeDefinition
    {
        private readonly GoodAmount[] _equipment;

        public AirWingTypeDefinition(
            UnitTypeId id,
            string key,
            long crew,
            GoodAmount[] equipment,
            Fixed64 airAttack,
            Fixed64 groundAttack,
            Fixed64 maxOrganization,
            Fixed64 rangeKm)
        {
            Id = id;
            Key = key;
            Crew = crew;
            _equipment = equipment ?? new GoodAmount[0];
            AirAttack = airAttack;
            GroundAttack = groundAttack;
            MaxOrganization = maxOrganization;
            RangeKm = rangeKm;
        }

        public UnitTypeId Id { get; }

        public string Key { get; }

        public long Crew { get; }

        public IReadOnlyList<GoodAmount> Equipment => _equipment;

        public Fixed64 AirAttack { get; }

        public Fixed64 GroundAttack { get; }

        public Fixed64 MaxOrganization { get; }

        public Fixed64 RangeKm { get; }
    }
}
