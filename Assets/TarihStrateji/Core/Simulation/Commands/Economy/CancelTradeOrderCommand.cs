using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    /// <summary>Kendi ticaret emrimizi iptal eder.</summary>
    public sealed class CancelTradeOrderCommand : GameCommand
    {
        public const string KindKey = "emir.ekonomi.ticaretEmriIptal";

        public CancelTradeOrderCommand(CountryId issuer, TradeOrderId order)
            : base(issuer)
        {
            Order = order;
        }

        public TradeOrderId Order { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
