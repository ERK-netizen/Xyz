using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>
    /// Doğrulanmış ekonomi tanımları ve parametreleri (içerik verisi; durum değildir, kayda
    /// girmez). <see cref="Create"/> tutarsız veriyi reddeder: askerî mal girdisiz bir tesisin
    /// çıktısı olamaz (CLAUDE.md §2.6 "yoktan üretilmez").
    /// </summary>
    public sealed class EconomyCatalog
    {
        private EconomyCatalog(
            StableIdMap<GoodId, GoodDefinition> goods,
            StableIdMap<FacilityTypeId, FacilityTypeDefinition> facilities,
            EconomyParameters parameters)
        {
            Goods = goods;
            FacilityTypes = facilities;
            Parameters = parameters;
        }

        public StableIdMap<GoodId, GoodDefinition> Goods { get; }

        public StableIdMap<FacilityTypeId, FacilityTypeDefinition> FacilityTypes { get; }

        public EconomyParameters Parameters { get; }

        public static Result<EconomyCatalog> Create(
            IReadOnlyList<GoodDefinition> goods,
            IReadOnlyList<FacilityTypeDefinition> facilityTypes,
            EconomyParameters parameters)
        {
            if (goods == null || facilityTypes == null || parameters == null)
            {
                throw new ArgumentNullException(goods == null ? nameof(goods) : facilityTypes == null ? nameof(facilityTypes) : nameof(parameters));
            }

            var goodMap = new StableIdMap<GoodId, GoodDefinition>(goods.Count);
            for (int i = 0; i < goods.Count; i++)
            {
                GoodDefinition g = goods[i];
                if (g == null || !g.Id.IsValid || string.IsNullOrEmpty(g.Key) || !g.BasePrice.IsPositive || !goodMap.TryAdd(g.Id, g))
                {
                    return Result<EconomyCatalog>.Failure(EconomyErrorKeys.TanimGecersiz);
                }
            }

            var facilityMap = new StableIdMap<FacilityTypeId, FacilityTypeDefinition>(facilityTypes.Count);
            for (int i = 0; i < facilityTypes.Count; i++)
            {
                FacilityTypeDefinition f = facilityTypes[i];
                if (f == null || !f.Id.IsValid || string.IsNullOrEmpty(f.Key) || f.Outputs.Count == 0 || f.WorkersRequired <= 0L
                    || f.BuildingSlots <= 0 || f.BuildDays <= 0 || f.BuildMoney.IsNegative || f.UpkeepPerDay.IsNegative
                    || !ValidAmounts(f.Inputs, goodMap) || !ValidAmounts(f.Outputs, goodMap)
                    || !ValidAmounts(f.BuildGoods, goodMap))
                {
                    return Result<EconomyCatalog>.Failure(EconomyErrorKeys.TanimGecersiz);
                }

                for (int o = 0; o < f.Outputs.Count; o++)
                {
                    if (goodMap[f.Outputs[o].Good].IsMilitary && f.Inputs.Count == 0)
                    {
                        return Result<EconomyCatalog>.Failure(EconomyErrorKeys.YoktanUretim);
                    }
                }

                if (!facilityMap.TryAdd(f.Id, f))
                {
                    return Result<EconomyCatalog>.Failure(EconomyErrorKeys.TanimGecersiz);
                }
            }

            if (!goodMap.TryGetValue(parameters.FoodGood, out GoodDefinition food) || food.Category != GoodCategory.Food)
            {
                return Result<EconomyCatalog>.Failure(EconomyErrorKeys.TanimGecersiz);
            }

            return Result<EconomyCatalog>.Success(new EconomyCatalog(goodMap, facilityMap, parameters));
        }

        private static bool ValidAmounts(IReadOnlyList<GoodAmount> amounts, StableIdMap<GoodId, GoodDefinition> goods)
        {
            for (int i = 0; i < amounts.Count; i++)
            {
                if (!amounts[i].Amount.IsPositive || !goods.ContainsKey(amounts[i].Good))
                {
                    return false;
                }

                for (int j = 0; j < i; j++)
                {
                    if (amounts[j].Good == amounts[i].Good)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
