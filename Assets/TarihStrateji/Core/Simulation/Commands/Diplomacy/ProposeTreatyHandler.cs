using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary><see cref="ProposeTreatyCommand"/>: teklif P1'de bekleyen (pending) duruma girer.</summary>
    public sealed class ProposeTreatyHandler : ContentHandler<ProposeTreatyCommand>
    {
        public ProposeTreatyHandler(GameContent content) : base(content) { }

        public override string Kind => ProposeTreatyCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, ProposeTreatyCommand c) =>
            DiplomacyRules.CheckPropose(state.Diplomacy, state.World, Content.Diplomacy, c.Proposal, c.Issuer, c.Target, c.Transfers, state.Today);

        protected override void ApplyTyped(CommandContext context, ProposeTreatyCommand c)
        {
            DiplomacyRules.Propose(context.State.Diplomacy, Content.Diplomacy, c.Proposal, c.Issuer, c.Target, c.Transfers, context.State.Today);
        }

        protected override void WriteTyped(ProposeTreatyCommand c, CanonicalWriter w)
        {
            w.WriteByte((byte)c.Proposal);
            w.WriteId(c.Target);
            w.WriteInt32(c.Transfers.Count);
            for (int i = 0; i < c.Transfers.Count; i++)
            {
                w.WriteId(c.Transfers[i].Region);
                w.WriteId(c.Transfers[i].NewOwner);
            }
        }

        protected override ProposeTreatyCommand ReadTyped(CountryId issuer, CanonicalReader r)
        {
            var kind = (ProposalKind)r.ReadByte();
            var target = new CountryId(r.ReadUInt64());
            var transfers = new RegionTransfer[r.ReadCount(r.Remaining)];
            for (int i = 0; i < transfers.Length; i++)
            {
                transfers[i] = new RegionTransfer(new RegionId(r.ReadUInt64()), new CountryId(r.ReadUInt64()));
            }

            return new ProposeTreatyCommand(issuer, kind, target, transfers);
        }
    }
}
