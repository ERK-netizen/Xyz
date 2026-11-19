using System.Collections.Generic;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Barış (bölge devirleriyle), saldırmazlık veya ittifak teklifi; muhatap kabul etmedikçe sonuç doğurmaz.</summary>
    public sealed class ProposeTreatyCommand : GameCommand
    {
        public const string KindKey = "emir.diplomasi.teklif";

        private readonly RegionTransfer[] _transfers;

        public ProposeTreatyCommand(CountryId issuer, ProposalKind proposal, CountryId target, RegionTransfer[] transfers)
            : base(issuer)
        {
            Proposal = proposal;
            Target = target;
            _transfers = transfers ?? new RegionTransfer[0];
        }

        public ProposalKind Proposal { get; }

        public CountryId Target { get; }

        public IReadOnlyList<RegionTransfer> Transfers => _transfers;

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
