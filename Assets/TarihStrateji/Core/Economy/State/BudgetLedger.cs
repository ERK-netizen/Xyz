using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>
    /// Bir ülkenin günlük para giriş/çıkış defteri (game-balance: musluk/gider defteri). Her gün
    /// P0'da sıfırlanır. Hazine değişimi = <see cref="Net"/> olmalıdır.
    /// </summary>
    public sealed class BudgetLedger
    {
        public Fixed64 TaxIncome { get; internal set; }

        public Fixed64 TradeIncome { get; internal set; }

        public Fixed64 ConstructionRefunds { get; internal set; }

        public Fixed64 TradeExpense { get; internal set; }

        public Fixed64 FacilityUpkeep { get; internal set; }

        public Fixed64 ConstructionSpending { get; internal set; }

        /// <summary>Kurum yatırımı vb. siyasi/idari harcamalar.</summary>
        public Fixed64 OtherSpending { get; internal set; }

        /// <summary>Olay ve odak gibi idari gelirler.</summary>
        public Fixed64 OtherIncome { get; internal set; }

        public Fixed64 Income => TaxIncome + TradeIncome + ConstructionRefunds + OtherIncome;

        public Fixed64 Expense => TradeExpense + FacilityUpkeep + ConstructionSpending + OtherSpending;

        public Fixed64 Net => Income - Expense;

        internal void Reset()
        {
            TaxIncome = Fixed64.Zero;
            TradeIncome = Fixed64.Zero;
            ConstructionRefunds = Fixed64.Zero;
            TradeExpense = Fixed64.Zero;
            FacilityUpkeep = Fixed64.Zero;
            ConstructionSpending = Fixed64.Zero;
            OtherSpending = Fixed64.Zero;
            OtherIncome = Fixed64.Zero;
        }
    }
}
