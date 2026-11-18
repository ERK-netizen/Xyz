using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// İnşaat kuyruğu (P9). Sipariş anında bedel (para + mal) ödenir ve bina slotları ayrılır;
    /// her gün sektör hâlâ sahibin elindeyse ilerleme = inşaat hızı (etkiler, taban 1 gün).
    /// Sektör işgaldeyse ilerleme durur. Tamamlanınca tesis oluşur; slotlar tesise geçer.
    /// </summary>
    public static class ConstructionRules
    {
        public static ValidationResult Validate(EconomyState economy, EconomyCatalog catalog, WorldMap map, CountryId country, FacilityTypeId typeId, SectorId sectorId)
        {
            ValidationResult placement = ValidatePlacement(economy, catalog, map, country, typeId, sectorId);
            if (!placement.IsValid)
            {
                return placement;
            }

            FacilityTypeDefinition type = catalog.FacilityTypes[typeId];
            CountryEconomy ce = economy.Countries[country];
            if (ce.Treasury < type.BuildMoney)
            {
                return ValidationResult.Fail(EconomyErrorKeys.HazineYetersiz);
            }

            for (int i = 0; i < type.BuildGoods.Count; i++)
            {
                if (ce.Stock(type.BuildGoods[i].Good) < type.BuildGoods[i].Amount)
                {
                    return ValidationResult.Fail(EconomyErrorKeys.StokYetersiz);
                }
            }

            return ValidationResult.Ok;
        }

        public static Result<ConstructionId> Order(EconomyState economy, EconomyCatalog catalog, WorldMap map, CountryId country, FacilityTypeId typeId, SectorId sectorId)
        {
            ValidationResult check = Validate(economy, catalog, map, country, typeId, sectorId);
            if (!check.IsValid)
            {
                return Result<ConstructionId>.Failure(check.ErrorKey);
            }

            FacilityTypeDefinition type = catalog.FacilityTypes[typeId];
            CountryEconomy ce = economy.Countries[country];
            ce.Treasury -= type.BuildMoney;
            ce.Ledger.ConstructionSpending += type.BuildMoney;
            for (int i = 0; i < type.BuildGoods.Count; i++)
            {
                ce.RemoveStock(type.BuildGoods[i].Good, type.BuildGoods[i].Amount);
            }

            map.UseBuildingSlots(sectorId, type.BuildingSlots);
            var id = new ConstructionId(economy.ConstructionIds.Allocate());
            economy.Constructions.Add(id, new ConstructionOrder(id, country, typeId, sectorId));
            return Result<ConstructionId>.Success(id);
        }

        /// <summary>
        /// Senaryo kurulumu: bedelsiz, hazır tesis yerleştirir. İnşaatla aynı yer kurallarından
        /// (sahip/kontrol, sektör türü, slot) geçer ve slotları ayırır; hazine ve stok aranmaz.
        /// </summary>
        public static Result<FacilityId> PlaceExisting(EconomyState economy, EconomyCatalog catalog, WorldMap map, CountryId country, FacilityTypeId typeId, SectorId sectorId)
        {
            ValidationResult placement = ValidatePlacement(economy, catalog, map, country, typeId, sectorId);
            if (!placement.IsValid)
            {
                return Result<FacilityId>.Failure(placement.ErrorKey);
            }

            map.UseBuildingSlots(sectorId, catalog.FacilityTypes[typeId].BuildingSlots);
            var id = new FacilityId(economy.FacilityIds.Allocate());
            economy.Facilities.Add(id, new Facility(id, typeId, sectorId, country));
            return Result<FacilityId>.Success(id);
        }

        /// <summary>Yer kuralları: ülke, tür, sektör var; sahip ve kontrol bizde; sektör türü uygun; slot yeterli.</summary>
        public static ValidationResult ValidatePlacement(EconomyState economy, EconomyCatalog catalog, WorldMap map, CountryId country, FacilityTypeId typeId, SectorId sectorId)
        {
            if (!economy.Countries.ContainsKey(country))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (!catalog.FacilityTypes.TryGetValue(typeId, out FacilityTypeDefinition type))
            {
                return ValidationResult.Fail(EconomyErrorKeys.TesisTuruBilinmiyor);
            }

            if (!map.Sectors.TryGetValue(sectorId, out CitySector sector))
            {
                return ValidationResult.Fail(EconomyErrorKeys.SektorBulunamadi);
            }

            if (sector.Owner != country || sector.Controller != country)
            {
                return ValidationResult.Fail(EconomyErrorKeys.SektorBizimDegil);
            }

            if (!type.AllowsSector(sector.Kind))
            {
                return ValidationResult.Fail(EconomyErrorKeys.SektorTuruUygunDegil);
            }

            return sector.FreeBuildingSlots < type.BuildingSlots ? ValidationResult.Fail(EconomyErrorKeys.SlotYetersiz) : ValidationResult.Ok;
        }

        public static ValidationResult CheckCancel(EconomyState economy, CountryId country, ConstructionId id)
        {
            if (!economy.Constructions.TryGetValue(id, out ConstructionOrder order))
            {
                return ValidationResult.Fail(EconomyErrorKeys.InsaatBulunamadi);
            }

            return order.Country != country ? ValidationResult.Fail(EconomyErrorKeys.InsaatBizimDegil) : ValidationResult.Ok;
        }

        /// <summary>İptal: slotlar bırakılır, paranın iade oranı kadarı geri ödenir, mallar iade edilmez.</summary>
        public static ValidationResult Cancel(EconomyState economy, EconomyCatalog catalog, WorldMap map, CountryId country, ConstructionId id)
        {
            ValidationResult check = CheckCancel(economy, country, id);
            if (!check.IsValid)
            {
                return check;
            }

            ConstructionOrder order = economy.Constructions[id];
            FacilityTypeDefinition type = catalog.FacilityTypes[order.Type];
            map.ReleaseBuildingSlots(order.Sector, type.BuildingSlots);
            Fixed64 refund = type.BuildMoney * catalog.Parameters.ConstructionRefundRatio;
            CountryEconomy ce = economy.Countries[country];
            ce.Treasury += refund;
            ce.Ledger.ConstructionRefunds += refund;
            economy.Constructions.Remove(id);
            return ValidationResult.Ok;
        }

        /// <summary>Günlük ilerleme; tamamlanan tesislerin kimlikleri artan sırayla eklenir.</summary>
        public static void AdvanceDay(EconomyState economy, EconomyCatalog catalog, WorldMap map, EffectEngine effects, GameDate today, List<FacilityId> completed)
        {
            var finished = new List<ConstructionId>();
            var targets = new EffectTarget[2];
            targets[0] = EffectTarget.Global;
            for (int i = 0; i < economy.Constructions.Count; i++)
            {
                ConstructionOrder order = economy.Constructions.ValueAt(i);
                if (!map.Sectors.TryGetValue(order.Sector, out CitySector sector) || sector.Controller != order.Country || sector.Owner != order.Country)
                {
                    continue;
                }

                targets[1] = EffectTarget.Of(order.Country);
                Fixed64 speed = Fixed64.Max(Fixed64.Zero, effects.Evaluate(targets, EconomyStatKeys.ConstructionSpeed, Fixed64.One, today));
                order.Progress += speed;
                if (order.Progress >= Fixed64.FromInt(catalog.FacilityTypes[order.Type].BuildDays))
                {
                    finished.Add(order.Id);
                }
            }

            for (int i = 0; i < finished.Count; i++)
            {
                ConstructionOrder order = economy.Constructions[finished[i]];
                economy.Constructions.Remove(order.Id);
                var facilityId = new FacilityId(economy.FacilityIds.Allocate());
                economy.Facilities.Add(facilityId, new Facility(facilityId, order.Type, order.Sector, order.Country));
                completed.Add(facilityId);
            }
        }
    }
}
