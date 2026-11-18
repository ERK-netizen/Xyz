using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    public sealed class PlaceTradeOrderHandler : CommandHandler<PlaceTradeOrderCommand>
    {
        private readonly EconomyCatalog _catalog;

        public PlaceTradeOrderHandler(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => PlaceTradeOrderCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, PlaceTradeOrderCommand c)
        {
            return TradeOrderRules.CheckPlace(state.Economy, _catalog, c.Issuer, c.Good, c.Side, c.QuantityPerDay, c.LimitPrice);
        }

        protected override void ApplyTyped(CommandContext context, PlaceTradeOrderCommand c)
        {
            TradeOrderRules.Place(context.State.Economy, _catalog, c.Issuer, c.Good, c.Side, c.QuantityPerDay, c.LimitPrice);
        }

        protected override void WriteTyped(PlaceTradeOrderCommand c, CanonicalWriter writer)
        {
            writer.WriteId(c.Good);
            writer.WriteByte((byte)c.Side);
            writer.WriteFixed64(c.QuantityPerDay);
            writer.WriteFixed64(c.LimitPrice);
        }

        protected override PlaceTradeOrderCommand ReadTyped(CountryId issuer, CanonicalReader reader)
        {
            return new PlaceTradeOrderCommand(issuer, new GoodId(reader.ReadUInt64()), (TradeSide)reader.ReadByte(), reader.ReadFixed64(), reader.ReadFixed64());
        }
    }
}
