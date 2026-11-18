using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    public sealed class CancelTradeOrderHandler : CommandHandler<CancelTradeOrderCommand>
    {
        public override string Kind => CancelTradeOrderCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, CancelTradeOrderCommand command)
        {
            return TradeOrderRules.CheckCancel(state.Economy, command.Issuer, command.Order);
        }

        protected override void ApplyTyped(CommandContext context, CancelTradeOrderCommand command)
        {
            TradeOrderRules.Cancel(context.State.Economy, command.Issuer, command.Order);
        }

        protected override void WriteTyped(CancelTradeOrderCommand command, CanonicalWriter writer) => writer.WriteId(command.Order);

        protected override CancelTradeOrderCommand ReadTyped(CountryId issuer, CanonicalReader reader) =>
            new CancelTradeOrderCommand(issuer, new TradeOrderId(reader.ReadUInt64()));
    }
}
