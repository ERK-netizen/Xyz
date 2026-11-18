using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>Ticaret emri verme/iptal ve vergi oranı politikası.</summary>
    public static class TradeOrderRules
    {
        /// <summary>Bir ülkenin aynı anda tutabileceği en fazla ticaret emri.</summary>
        public const int MaxOrdersPerCountry = 64;

        public static ValidationResult CheckPlace(EconomyState economy, EconomyCatalog catalog, CountryId country, GoodId good, TradeSide side, Fixed64 quantityPerDay, Fixed64 limitPrice)
        {
            if (!economy.Countries.ContainsKey(country))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (!catalog.Goods.ContainsKey(good))
            {
                return ValidationResult.Fail(EconomyErrorKeys.MalBilinmiyor);
            }

            if ((side != TradeSide.Buy && side != TradeSide.Sell) || !quantityPerDay.IsPositive || !limitPrice.IsPositive)
            {
                return ValidationResult.Fail(EconomyErrorKeys.TicaretEmriGecersiz);
            }

            int count = 0;
            for (int i = 0; i < economy.TradeOrders.Count; i++)
            {
                if (economy.TradeOrders.ValueAt(i).Country == country)
                {
                    count++;
                }
            }

            return count >= MaxOrdersPerCountry ? ValidationResult.Fail(EconomyErrorKeys.TicaretEmriGecersiz) : ValidationResult.Ok;
        }

        public static Result<TradeOrderId> Place(EconomyState economy, EconomyCatalog catalog, CountryId country, GoodId good, TradeSide side, Fixed64 quantityPerDay, Fixed64 limitPrice)
        {
            ValidationResult check = CheckPlace(economy, catalog, country, good, side, quantityPerDay, limitPrice);
            if (!check.IsValid)
            {
                return Result<TradeOrderId>.Failure(check.ErrorKey);
            }

            var id = new TradeOrderId(economy.TradeOrderIds.Allocate());
            economy.TradeOrders.Add(id, new TradeOrder(id, country, good, side, quantityPerDay, limitPrice));
            return Result<TradeOrderId>.Success(id);
        }

        public static ValidationResult CheckCancel(EconomyState economy, CountryId country, TradeOrderId id)
        {
            if (!economy.TradeOrders.TryGetValue(id, out TradeOrder order))
            {
                return ValidationResult.Fail(EconomyErrorKeys.TicaretEmriBulunamadi);
            }

            return order.Country != country ? ValidationResult.Fail(EconomyErrorKeys.TicaretEmriBulunamadi) : ValidationResult.Ok;
        }

        public static ValidationResult Cancel(EconomyState economy, CountryId country, TradeOrderId id)
        {
            ValidationResult check = CheckCancel(economy, country, id);
            if (check.IsValid)
            {
                economy.TradeOrders.Remove(id);
            }

            return check;
        }

        public static ValidationResult CheckTaxRate(EconomyState economy, EconomyCatalog catalog, CountryId country, Fixed64 rate)
        {
            if (!economy.Countries.ContainsKey(country))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            return rate.IsNegative || rate > catalog.Parameters.MaxTaxRate ? ValidationResult.Fail(EconomyErrorKeys.VergiOraniGecersiz) : ValidationResult.Ok;
        }

        public static ValidationResult SetTaxRate(EconomyState economy, EconomyCatalog catalog, CountryId country, Fixed64 rate)
        {
            ValidationResult check = CheckTaxRate(economy, catalog, country, rate);
            if (check.IsValid)
            {
                economy.Countries[country].TaxRate = rate;
            }

            return check;
        }
    }
}
