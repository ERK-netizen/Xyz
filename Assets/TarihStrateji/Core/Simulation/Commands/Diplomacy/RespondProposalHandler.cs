using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Simulation.Systems.Geo;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>
    /// <see cref="RespondProposalCommand"/>: kabul P1'de koşullar yeniden denetlenerek uygulanır;
    /// kabul edilen barıştan sonra birlikler <see cref="PeaceSettlement"/> ile yerleştirilir.
    /// </summary>
    public sealed class RespondProposalHandler : ContentHandler<RespondProposalCommand>
    {
        public RespondProposalHandler(GameContent content) : base(content) { }

        public override string Kind => RespondProposalCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, RespondProposalCommand c) =>
            DiplomacyRules.CheckRespond(state.Diplomacy, state.World, Content.Diplomacy, c.Issuer, c.Proposal, c.Accept, state.Today);

        protected override void ApplyTyped(CommandContext context, RespondProposalCommand c)
        {
            GameState state = context.State;
            if (DiplomacyRules.Respond(state.Diplomacy, state.World, Content.Diplomacy, c.Proposal, c.Accept, state.Today))
            {
                PeaceSettlement.Relocate(GeoWarfareContexts.From(state, Content), state.Diplomacy, new List<string>());
            }
        }

        protected override void WriteTyped(RespondProposalCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Proposal);
            w.WriteBool(c.Accept);
        }

        protected override RespondProposalCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new RespondProposalCommand(issuer, new ProposalId(r.ReadUInt64()), r.ReadBool());
    }
}
