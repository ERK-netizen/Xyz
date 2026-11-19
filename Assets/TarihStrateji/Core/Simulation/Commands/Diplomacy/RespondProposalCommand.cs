using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Bize yapılmış bekleyen teklifi kabul veya reddeder (iki taraflı onayın ikinci yarısı).</summary>
    public sealed class RespondProposalCommand : GameCommand
    {
        public const string KindKey = "emir.diplomasi.teklifYaniti";

        public RespondProposalCommand(CountryId issuer, ProposalId proposal, bool accept)
            : base(issuer)
        {
            Proposal = proposal;
            Accept = accept;
        }

        public ProposalId Proposal { get; }

        public bool Accept { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
