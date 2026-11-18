using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    /// <summary>Dünya pazarına kalıcı günlük alış veya satış emri verir.</summary>
    public sealed class PlaceTradeOrderCommand : GameCommand
    {
        public const string KindKey = "emir.ekonomi.ticaretEmri";

        public PlaceTradeOrderCommand(CountryId issuer, GoodId good, TradeSide side, Fixed64 quantityPerDay, Fixed64 limitPrice)
            : base(issuer)
        {
            Good = good;
            Side = side;
            QuantityPerDay = quantityPerDay;
            LimitPrice = limitPrice;
        }

        public GoodId Good { get; }

        public TradeSide Side { get; }

        public Fixed64 QuantityPerDay { get; }

        public Fixed64 LimitPrice { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
