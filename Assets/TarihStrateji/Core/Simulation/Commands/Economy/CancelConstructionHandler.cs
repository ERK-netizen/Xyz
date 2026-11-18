using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    public sealed class CancelConstructionHandler : CommandHandler<CancelConstructionCommand>
    {
        private readonly EconomyCatalog _catalog;

        public CancelConstructionHandler(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => CancelConstructionCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, CancelConstructionCommand command)
        {
            return ConstructionRules.CheckCancel(state.Economy, command.Issuer, command.Construction);
        }

        protected override void ApplyTyped(CommandContext context, CancelConstructionCommand command)
        {
            ConstructionRules.Cancel(context.State.Economy, _catalog, context.State.World, command.Issuer, command.Construction);
        }

        protected override void WriteTyped(CancelConstructionCommand command, CanonicalWriter writer) => writer.WriteId(command.Construction);

        protected override CancelConstructionCommand ReadTyped(CountryId issuer, CanonicalReader reader) =>
            new CancelConstructionCommand(issuer, new ConstructionId(reader.ReadUInt64()));
    }
}
