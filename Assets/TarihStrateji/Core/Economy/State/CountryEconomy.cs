using System;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>
    /// Ülke ekonomisi: hazine, vergi oranı ve **ortak envanter** (CLAUDE.md §2.6: üretilen her mal
    /// buraya girer, tüketilen her mal buradan çekilir). Envanter asla negatif olmaz.
    /// </summary>
    public sealed class CountryEconomy
    {
        internal CountryEconomy(CountryId country)
        {
            Country = country;
        }

        public CountryId Country { get; }

        /// <summary>Hazine; açık (negatif) olabilir.</summary>
        public Fixed64 Treasury { get; internal set; }

        public Fixed64 TaxRate { get; internal set; }

        public StableIdMap<GoodId, Fixed64> Inventory { get; } = new StableIdMap<GoodId, Fixed64>();

        /// <summary>Dünkü gıda açığı oranı [0, 1]; P6 ölüm hazardını artırır.</summary>
        public Fixed64 FoodShortage { get; internal set; }

        /// <summary>Bugün üretilen malların temel fiyatla değeri (vergi tabanı).</summary>
        public Fixed64 OutputValueToday { get; internal set; }

        public BudgetLedger Ledger { get; } = new BudgetLedger();

        public Fixed64 Stock(GoodId good)
        {
            return Inventory.TryGetValue(good, out Fixed64 amount) ? amount : Fixed64.Zero;
        }

        internal void AddStock(GoodId good, Fixed64 amount)
        {
            if (amount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (amount.IsZero)
            {
                return;
            }

            Inventory[good] = Stock(good) + amount;
        }

        internal void RemoveStock(GoodId good, Fixed64 amount)
        {
            Fixed64 current = Stock(good);
            if (amount.IsNegative || amount > current)
            {
                throw new InvalidOperationException(EconomyErrorKeys.NegatifStok);
            }

            if (amount.IsZero)
            {
                return;
            }

            Inventory[good] = current - amount;
        }
    }
}
