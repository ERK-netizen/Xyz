using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Diğer modüllerin (siyaset vb.) hazineden harcaması. Her harcama deftere yazılır; böylece
    /// "hazine değişimi = defter neti" değişmezi korunur.
    /// </summary>
    public static class TreasuryRules
    {
        public static ValidationResult CheckSpend(EconomyState economy, CountryId country, Fixed64 amount)
        {
            if (!economy.Countries.TryGetValue(country, out CountryEconomy ce))
            {
                return ValidationResult.Fail(EconomyErrorKeys.UlkeEkonomisiYok);
            }

            if (!amount.IsPositive)
            {
                return ValidationResult.Fail(EconomyErrorKeys.TanimGecersiz);
            }

            return ce.Treasury < amount ? ValidationResult.Fail(EconomyErrorKeys.HazineYetersiz) : ValidationResult.Ok;
        }

        public static ValidationResult Spend(EconomyState economy, CountryId country, Fixed64 amount)
        {
            ValidationResult check = CheckSpend(economy, country, amount);
            if (check.IsValid)
            {
                CountryEconomy ce = economy.Countries[country];
                ce.Treasury -= amount;
                ce.Ledger.OtherSpending += amount;
            }

            return check;
        }
    }
}
