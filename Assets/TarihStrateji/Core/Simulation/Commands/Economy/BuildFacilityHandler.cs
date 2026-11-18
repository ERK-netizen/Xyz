using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Economy
{
    public sealed class BuildFacilityHandler : CommandHandler<BuildFacilityCommand>
    {
        private readonly EconomyCatalog _catalog;

        public BuildFacilityHandler(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => BuildFacilityCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, BuildFacilityCommand command)
        {
            return ConstructionRules.Validate(state.Economy, _catalog, state.World, command.Issuer, command.FacilityType, command.Sector);
        }

        protected override void ApplyTyped(CommandContext context, BuildFacilityCommand command)
        {
            ConstructionRules.Order(context.State.Economy, _catalog, context.State.World, command.Issuer, command.FacilityType, command.Sector);
        }

        protected override void WriteTyped(BuildFacilityCommand command, CanonicalWriter writer)
        {
            writer.WriteId(command.FacilityType);
            writer.WriteId(command.Sector);
        }

        protected override BuildFacilityCommand ReadTyped(CountryId issuer, CanonicalReader reader)
        {
            return new BuildFacilityCommand(issuer, new FacilityTypeId(reader.ReadUInt64()), new SectorId(reader.ReadUInt64()));
        }
    }
}
