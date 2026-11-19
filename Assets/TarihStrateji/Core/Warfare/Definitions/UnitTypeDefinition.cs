using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>
    /// Kara birliği türü (içerik verisi). Tam güçteki asker ve ekipman; ekipman ortak envanterden
    /// çekilir (yoktan üretilmez), asker bölge nüfusundan seferber edilir.
    /// </summary>
    public sealed class UnitTypeDefinition
    {
        private readonly GoodAmount[] _equipment;

        public UnitTypeDefinition(
            UnitTypeId id,
            string key,
            long manpower,
            GoodAmount[] equipment,
            Fixed64 attack,
            Fixed64 defense,
            Fixed64 maxOrganization,
            Fixed64 speedPerDay,
            Fixed64 actionPointsPerDay,
            Fixed64 attackActionCost)
        {
            Id = id;
            Key = key;
            Manpower = manpower;
            _equipment = equipment ?? new GoodAmount[0];
            Attack = attack;
            Defense = defense;
            MaxOrganization = maxOrganization;
            SpeedPerDay = speedPerDay;
            ActionPointsPerDay = actionPointsPerDay;
            AttackActionCost = attackActionCost;
        }

        public UnitTypeId Id { get; }

        public string Key { get; }

        public long Manpower { get; }

        public IReadOnlyList<GoodAmount> Equipment => _equipment;

        public Fixed64 Attack { get; }

        public Fixed64 Defense { get; }

        public Fixed64 MaxOrganization { get; }

        /// <summary>Günlük hareket bütçesi (hareket maliyeti birimi: km × arazi × altyapı).</summary>
        public Fixed64 SpeedPerDay { get; }

        public Fixed64 ActionPointsPerDay { get; }

        /// <summary>Gün içi anında saldırının AP bedeli.</summary>
        public Fixed64 AttackActionCost { get; }
    }
}
