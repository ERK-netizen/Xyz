using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    public sealed class SetResearchHandler : CommandHandler<SetResearchCommand>
    {
        private readonly TechCatalog _catalog;

        public SetResearchHandler(TechCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => SetResearchCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, SetResearchCommand c) =>
            ResearchRules.CheckSetCurrent(state.Research, _catalog, c.Issuer, c.Technology, state.Today);

        protected override void ApplyTyped(CommandContext context, SetResearchCommand c) =>
            ResearchRules.SetCurrent(context.State.Research, _catalog, c.Issuer, c.Technology, context.State.Today);

        protected override void WriteTyped(SetResearchCommand c, CanonicalWriter w) => w.WriteId(c.Technology);

        protected override SetResearchCommand ReadTyped(CountryId issuer, CanonicalReader r) => new SetResearchCommand(issuer, new TechnologyId(r.ReadUInt64()));
    }
}
