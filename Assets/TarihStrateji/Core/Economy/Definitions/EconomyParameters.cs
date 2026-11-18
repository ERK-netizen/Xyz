using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>Ekonomi genel parametreleri (içerik verisi).</summary>
    public sealed class EconomyParameters
    {
        public EconomyParameters(
            GoodId foodGood,
            Fixed64 foodPerThousandPerDay,
            Fixed64 maxTaxRate,
            Fixed64 constructionRefundRatio,
            Fixed64 marketPriceStep,
            Fixed64 minPriceFactor,
            Fixed64 maxPriceFactor)
        {
            if (!foodGood.IsValid || foodPerThousandPerDay.IsNegative || maxTaxRate.IsNegative || maxTaxRate > Fixed64.One
                || constructionRefundRatio.IsNegative || constructionRefundRatio > Fixed64.One || marketPriceStep.IsNegative
                || !minPriceFactor.IsPositive || minPriceFactor > Fixed64.One || maxPriceFactor < Fixed64.One)
            {
                throw new ArgumentException(EconomyErrorKeys.TanimGecersiz);
            }

            FoodGood = foodGood;
            FoodPerThousandPerDay = foodPerThousandPerDay;
            MaxTaxRate = maxTaxRate;
            ConstructionRefundRatio = constructionRefundRatio;
            MarketPriceStep = marketPriceStep;
            MinPriceFactor = minPriceFactor;
            MaxPriceFactor = maxPriceFactor;
        }

        /// <summary>Nüfusun tükettiği gıda malı.</summary>
        public GoodId FoodGood { get; }

        /// <summary>1000 kişinin günlük gıda ihtiyacı (mal birimi).</summary>
        public Fixed64 FoodPerThousandPerDay { get; }

        public Fixed64 MaxTaxRate { get; }

        /// <summary>İptal edilen inşaatın para iadesi oranı (mallar iade edilmez).</summary>
        public Fixed64 ConstructionRefundRatio { get; }

        /// <summary>Günlük fiyat ayarı: fiyat × (1 + adım × dengesizlik), dengesizlik ∈ [−1, 1].</summary>
        public Fixed64 MarketPriceStep { get; }

        public Fixed64 MinPriceFactor { get; }

        public Fixed64 MaxPriceFactor { get; }
    }
}
