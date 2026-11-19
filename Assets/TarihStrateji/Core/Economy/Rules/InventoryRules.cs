using System.Collections.Generic;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Diğer modüllerin (ordu, donanma…) ortak envanterden mal çekmesi ve iadesi. Mal yalnız
    /// envanterde varsa çekilir (CLAUDE.md §2.6: ekipman yoktan üretilmez); iade yalnız daha önce
    /// çekilmiş malın geri dönüşüdür.
    /// </summary>
    public static class InventoryRules
    {
        /// <summary><paramref name="amounts"/> × <paramref name="factor"/> stokta var mı.</summary>
        public static ValidationResult CheckAvailable(EconomyState economy, CountryId country, IReadOnlyList<GoodAmount> amounts, Fixed64 factor)
        {
            if (!economy.Countries.TryGetValue(country, out CountryEconomy ce))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (factor.IsNegative)
            {
                return ValidationResult.Fail(EconomyErrorKeys.TanimGecersiz);
            }

            for (int i = 0; i < amounts.Count; i++)
            {
                if (ce.Stock(amounts[i].Good) < amounts[i].Amount * factor)
                {
                    return ValidationResult.Fail(EconomyErrorKeys.StokYetersiz);
                }
            }

            return ValidationResult.Ok;
        }

        public static ValidationResult Consume(EconomyState economy, CountryId country, IReadOnlyList<GoodAmount> amounts, Fixed64 factor)
        {
            ValidationResult check = CheckAvailable(economy, country, amounts, factor);
            if (check.IsValid)
            {
                CountryEconomy ce = economy.Countries[country];
                for (int i = 0; i < amounts.Count; i++)
                {
                    ce.RemoveStock(amounts[i].Good, amounts[i].Amount * factor);
                }
            }

            return check;
        }

        /// <summary>Daha önce çekilmiş malın (kısmi) iadesi.</summary>
        public static ValidationResult Return(EconomyState economy, CountryId country, IReadOnlyList<GoodAmount> amounts, Fixed64 factor)
        {
            if (!economy.Countries.TryGetValue(country, out CountryEconomy ce))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (factor.IsNegative)
            {
                return ValidationResult.Fail(EconomyErrorKeys.TanimGecersiz);
            }

            for (int i = 0; i < amounts.Count; i++)
            {
                ce.AddStock(amounts[i].Good, amounts[i].Amount * factor);
            }

            return ValidationResult.Ok;
        }
    }
}
