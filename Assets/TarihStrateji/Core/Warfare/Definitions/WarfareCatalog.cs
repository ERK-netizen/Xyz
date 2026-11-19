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
        private WarfareCatalog(
            StableIdMap<UnitTypeId, UnitTypeDefinition> unitTypes,
            StableIdMap<UnitTypeId, ShipTypeDefinition> shipTypes,
            StableIdMap<UnitTypeId, AirWingTypeDefinition> airWingTypes,
            WarfareParameters parameters,
            JointWarfareParameters joint)
        {
            UnitTypes = unitTypes;
            ShipTypes = shipTypes;
            AirWingTypes = airWingTypes;
            Parameters = parameters;
            Joint = joint;
        }

        public StableIdMap<UnitTypeId, UnitTypeDefinition> UnitTypes { get; }

        public StableIdMap<UnitTypeId, ShipTypeDefinition> ShipTypes { get; }

        public StableIdMap<UnitTypeId, AirWingTypeDefinition> AirWingTypes { get; }

        public WarfareParameters Parameters { get; }

        public JointWarfareParameters Joint { get; }

        /// <summary>Ekipman malları ekonomi kataloğunda tanımlı ve askerî olmalı.</summary>
        /// <summary>
        /// Kara, deniz ve hava türlerini doğrular. Tür kimlikleri üç küme arasında da tekildir;
        /// tüm teçhizat askerî maldır (gemi ve uçak dahil yoktan üretilmez).
        /// </summary>
        public static Result<WarfareCatalog> Create(
            IReadOnlyList<UnitTypeDefinition> unitTypes,
            IReadOnlyList<ShipTypeDefinition> shipTypes,
            IReadOnlyList<AirWingTypeDefinition> airWingTypes,
            WarfareParameters parameters,
            JointWarfareParameters joint,
            EconomyCatalog economy)
        {
            if (unitTypes == null || shipTypes == null || airWingTypes == null || parameters == null || joint == null || economy == null)
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

                if (!ValidEquipment(t.Equipment, economy))
                {
                    return Result<WarfareCatalog>.Failure(WarfareErrorKeys.TanimGecersiz);
                }
            }

            var ships = new StableIdMap<UnitTypeId, ShipTypeDefinition>(shipTypes.Count);
            for (int i = 0; i < shipTypes.Count; i++)
            {
                ShipTypeDefinition t = shipTypes[i];
                if (t == null || !t.Id.IsValid || map.ContainsKey(t.Id) || string.IsNullOrEmpty(t.Key) || t.Crew <= 0L
                    || t.Attack.IsNegative || !t.MaxOrganization.IsPositive || !t.SpeedPerDay.IsPositive
                    || t.TransportCapacity < 0 || t.ShoreBombardment.IsNegative || !ValidEquipment(t.Equipment, economy) || !ships.TryAdd(t.Id, t))
                {
                    return Result<WarfareCatalog>.Failure(WarfareErrorKeys.TanimGecersiz);
                }
            }

            var air = new StableIdMap<UnitTypeId, AirWingTypeDefinition>(airWingTypes.Count);
            for (int i = 0; i < airWingTypes.Count; i++)
            {
                AirWingTypeDefinition t = airWingTypes[i];
                if (t == null || !t.Id.IsValid || map.ContainsKey(t.Id) || ships.ContainsKey(t.Id) || string.IsNullOrEmpty(t.Key) || t.Crew <= 0L
                    || t.AirAttack.IsNegative || t.GroundAttack.IsNegative || !(t.AirAttack + t.GroundAttack).IsPositive
                    || !t.MaxOrganization.IsPositive || !t.RangeKm.IsPositive || !ValidEquipment(t.Equipment, economy) || !air.TryAdd(t.Id, t))
                {
                    return Result<WarfareCatalog>.Failure(WarfareErrorKeys.TanimGecersiz);
                }
            }

            return Result<WarfareCatalog>.Success(new WarfareCatalog(map, ships, air, parameters, joint));
        }

        private static bool ValidEquipment(IReadOnlyList<GoodAmount> equipment, EconomyCatalog economy)
        {
            if (equipment.Count == 0)
            {
                return false;
            }

            for (int e = 0; e < equipment.Count; e++)
            {
                if (!equipment[e].Amount.IsPositive || !economy.Goods.TryGetValue(equipment[e].Good, out GoodDefinition good) || !good.IsMilitary)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
