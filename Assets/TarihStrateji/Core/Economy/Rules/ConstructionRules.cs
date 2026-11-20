using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Economy.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// İnşaat kuyruğu (P9). Sipariş anında bedel (para + mal) ödenir ve şehir slotları ayrılır
    /// (süren inşaat slot sayılır); her gün şehir hâlâ sahibin elinde ve kontrolündeyse ilerleme =
    /// inşaat hızı (etkiler, taban 1 gün). İşgalde ilerleme durur. Tamamlanınca tesis oluşur.
    /// </summary>
    public static class ConstructionRules
    {
        public static ValidationResult Validate(EconomyState economy, EconomyCatalog catalog, GeoWorld world, CountryId country, FacilityTypeId typeId, CityId cityId)
        {
            ValidationResult placement = ValidatePlacement(economy, catalog, world, country, typeId, cityId);
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

        public static Result<ConstructionId> Order(EconomyState economy, EconomyCatalog catalog, GeoWorld world, CountryId country, FacilityTypeId typeId, CityId cityId)
        {
            ValidationResult check = Validate(economy, catalog, world, country, typeId, cityId);
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

            var id = new ConstructionId(economy.ConstructionIds.Allocate());
            economy.Constructions.Add(id, new ConstructionOrder(id, country, typeId, cityId));
            return Result<ConstructionId>.Success(id);
        }

        /// <summary>
        /// Senaryo kurulumu: bedelsiz, hazır tesis yerleştirir. İnşaatla aynı yer kurallarından
        /// (sahip/kontrol, liman, slot) geçer ve slotları ayırır; hazine ve stok aranmaz.
        /// </summary>
        public static Result<FacilityId> PlaceExisting(EconomyState economy, EconomyCatalog catalog, GeoWorld world, CountryId country, FacilityTypeId typeId, CityId cityId)
        {
            ValidationResult placement = ValidatePlacement(economy, catalog, world, country, typeId, cityId);
            if (!placement.IsValid)
            {
                return Result<FacilityId>.Failure(placement.ErrorKey);
            }

            var id = new FacilityId(economy.FacilityIds.Allocate());
            economy.Facilities.Add(id, new Facility(id, typeId, cityId, country));
            return Result<FacilityId>.Success(id);
        }

        /// <summary>Yer kuralları: ülke, tür, şehir var; sahip ve kontrol bizde; liman gerekiyorsa liman; slot yeterli.</summary>
        public static ValidationResult ValidatePlacement(EconomyState economy, EconomyCatalog catalog, GeoWorld world, CountryId country, FacilityTypeId typeId, CityId cityId)
        {
            if (!economy.Countries.ContainsKey(country))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (!catalog.FacilityTypes.TryGetValue(typeId, out FacilityTypeDefinition type))
            {
                return ValidationResult.Fail(EconomyErrorKeys.TesisTuruBilinmiyor);
            }

            if (!world.TryGetCity(cityId, out City city))
            {
                return ValidationResult.Fail(EconomyErrorKeys.SehirBulunamadi);
            }

            if (type.RequiresPort && !world.IsPort(city))
            {
                return ValidationResult.Fail(EconomyErrorKeys.LimanGerekli);
            }

            return CitySlotRules.CheckBuild(city, country, EconomyGeography.UsedSlots(economy, catalog, cityId), type.BuildingSlots,
                catalog.Parameters.BuildingSlotsPerDevelopment);
        }

        public static ValidationResult CheckCancel(EconomyState economy, CountryId country, ConstructionId id)
        {
            if (!economy.Constructions.TryGetValue(id, out ConstructionOrder order))
            {
                return ValidationResult.Fail(EconomyErrorKeys.InsaatBulunamadi);
            }

            return order.Country != country ? ValidationResult.Fail(EconomyErrorKeys.InsaatBizimDegil) : ValidationResult.Ok;
        }

        /// <summary>İptal: slotlar boşalır, paranın iade oranı kadarı geri ödenir, mallar iade edilmez.</summary>
        public static ValidationResult Cancel(EconomyState economy, EconomyCatalog catalog, GeoWorld world, CountryId country, ConstructionId id)
        {
            ValidationResult check = CheckCancel(economy, country, id);
            if (!check.IsValid)
            {
                return check;
            }

            ConstructionOrder order = economy.Constructions[id];
            FacilityTypeDefinition type = catalog.FacilityTypes[order.Type];
            Fixed64 refund = type.BuildMoney * catalog.Parameters.ConstructionRefundRatio;
            CountryEconomy ce = economy.Countries[country];
            ce.Treasury += refund;
            ce.Ledger.ConstructionRefunds += refund;
            economy.Constructions.Remove(id);
            return ValidationResult.Ok;
        }

        /// <summary>Günlük ilerleme; tamamlanan tesislerin kimlikleri artan sırayla eklenir.</summary>
        public static void AdvanceDay(EconomyState economy, EconomyCatalog catalog, GeoWorld world, EffectEngine effects, GameDate today, List<FacilityId> completed)
        {
            var finished = new List<ConstructionId>();
            var targets = new EffectTarget[2];
            targets[0] = EffectTarget.Global;
            for (int i = 0; i < economy.Constructions.Count; i++)
            {
                ConstructionOrder order = economy.Constructions.ValueAt(i);
                if (!world.TryGetCity(order.City, out City city) || !CitySlotRules.IsOperational(city, order.Country))
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
                economy.Facilities.Add(facilityId, new Facility(facilityId, order.Type, order.City, order.Country));
                completed.Add(facilityId);
            }
        }
    }
}
