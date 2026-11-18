using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    public sealed class EnqueueResearchHandler : CommandHandler<EnqueueResearchCommand>
    {
        private readonly TechCatalog _catalog;

        public EnqueueResearchHandler(TechCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public override string Kind => EnqueueResearchCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, EnqueueResearchCommand c) =>
            ResearchRules.CheckEnqueue(state.Research, _catalog, c.Issuer, c.Technology);

        protected override void ApplyTyped(CommandContext context, EnqueueResearchCommand c) =>
            ResearchRules.Enqueue(context.State.Research, _catalog, c.Issuer, c.Technology);

        protected override void WriteTyped(EnqueueResearchCommand c, CanonicalWriter w) => w.WriteId(c.Technology);

        protected override EnqueueResearchCommand ReadTyped(CountryId issuer, CanonicalReader r) => new EnqueueResearchCommand(issuer, new TechnologyId(r.ReadUInt64()));
    }
}
