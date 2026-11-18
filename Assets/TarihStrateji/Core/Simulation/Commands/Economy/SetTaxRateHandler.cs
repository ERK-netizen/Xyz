using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    public sealed class SetTaxRateHandler : CommandHandler<SetTaxRateCommand>
    {
        private readonly EconomyCatalog _catalog;

        public SetTaxRateHandler(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => SetTaxRateCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SetTaxRateCommand command)
        {
            return TradeOrderRules.CheckTaxRate(state.Economy, _catalog, command.Issuer, command.Rate);
        }

        protected override void ApplyTyped(CommandContext context, SetTaxRateCommand command)
        {
            TradeOrderRules.SetTaxRate(context.State.Economy, _catalog, command.Issuer, command.Rate);
        }

        protected override void WriteTyped(SetTaxRateCommand command, CanonicalWriter writer) => writer.WriteFixed64(command.Rate);

        protected override SetTaxRateCommand ReadTyped(CountryId issuer, CanonicalReader reader) => new SetTaxRateCommand(issuer, reader.ReadFixed64());
    }
}
