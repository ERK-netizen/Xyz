using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>
    /// İki taraflı onay bekleyen teklif (CLAUDE.md §2.6: barış `pending → accepted`). Yalnız
    /// muhatap (<see cref="To"/>) kabul veya ret eder; yanıtsız teklif <see cref="LastDay"/>'den sonra düşer.
    /// </summary>
    public sealed class Proposal
    {
        internal Proposal(ProposalId id, ProposalKind kind, CountryId from, CountryId to, GameDate created, GameDate lastDay)
        {
            Id = id;
            Kind = kind;
            From = from;
            To = to;
            Created = created;
            LastDay = lastDay;
            Status = ProposalStatus.Pending;
        }

        public ProposalId Id { get; }

        public ProposalKind Kind { get; }

        public CountryId From { get; }

        public CountryId To { get; }

        public GameDate Created { get; }

        public GameDate LastDay { get; }

        public ProposalStatus Status { get; internal set; }

        internal List<RegionTransfer> TransferList { get; } = new List<RegionTransfer>();

        /// <summary>Barış maddeleri (yalnız <see cref="ProposalKind.Peace"/>).</summary>
        public IReadOnlyList<RegionTransfer> Transfers => TransferList;
    }
}
