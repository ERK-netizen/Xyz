using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Definitions;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>Doğrulanmış birlik türleri ve muharebe parametreleri.</summary>
    public sealed class WarfareCatalog
    {
        private WarfareCatalog(StableIdMap<UnitTypeId, UnitTypeDefinition> unitTypes, WarfareParameters parameters)
        {
            UnitTypes = unitTypes;
            Parameters = parameters;
        }

        public StableIdMap<UnitTypeId, UnitTypeDefinition> UnitTypes { get; }

        public WarfareParameters Parameters { get; }

        /// <summary>Ekipman malları ekonomi kataloğunda tanımlı ve askerî olmalı.</summary>
        public static Result<WarfareCatalog> Create(IReadOnlyList<UnitTypeDefinition> unitTypes, WarfareParameters parameters, EconomyCatalog economy)
        {
            if (unitTypes == null || parameters == null || economy == null)
            {
                throw new ArgumentNullException(nameof(unitTypes));
            }

            var map = new StableIdMap<UnitTypeId, UnitTypeDefinition>(unitTypes.Count);
            for (int i = 0; i < unitTypes.Count; i++)
            {
                UnitTypeDefinition t = unitTypes[i];
                if (t == null || !t.Id.IsValid || string.IsNullOrEmpty(t.Key) || t.Manpower <= 0L || t.Equipment.Count == 0
                    || !t.Attack.IsPositive || !t.Defense.IsPositive || !t.MaxOrganization.IsPositive || !t.SpeedPerDay.IsPositive
                    || t.ActionPointsPerDay.IsNegative || t.AttackActionCost.IsNegative || !map.TryAdd(t.Id, t))
                {
                    return Result<WarfareCatalog>.Failure(WarfareErrorKeys.TanimGecersiz);
                }

                for (int e = 0; e < t.Equipment.Count; e++)
                {
                    if (!t.Equipment[e].Amount.IsPositive || !economy.Goods.TryGetValue(t.Equipment[e].Good, out GoodDefinition good) || !good.IsMilitary)
                    {
                        return Result<WarfareCatalog>.Failure(WarfareErrorKeys.TanimGecersiz);
                    }
                }
            }

            return Result<WarfareCatalog>.Success(new WarfareCatalog(map, parameters));
        }
    }
}
