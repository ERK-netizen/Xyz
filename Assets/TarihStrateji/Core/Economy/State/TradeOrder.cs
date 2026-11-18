using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>
    /// Kalıcı günlük ticaret emri: her gün en fazla <see cref="QuantityPerDay"/> kadar, alışta en
    /// fazla, satışta en az <see cref="LimitPrice"/> fiyatla.
    /// </summary>
    public sealed class TradeOrder
    {
        internal TradeOrder(TradeOrderId id, CountryId country, GoodId good, TradeSide side, Fixed64 quantityPerDay, Fixed64 limitPrice)
        {
            Id = id;
            Country = country;
            Good = good;
            Side = side;
            QuantityPerDay = quantityPerDay;
            LimitPrice = limitPrice;
        }

        public TradeOrderId Id { get; }

        public CountryId Country { get; }

        public GoodId Good { get; }

        public TradeSide Side { get; }

        public Fixed64 QuantityPerDay { get; }

        public Fixed64 LimitPrice { get; }

        /// <summary>Bugün gerçekleşen miktar.</summary>
        public Fixed64 FilledToday { get; internal set; }
    }
}
