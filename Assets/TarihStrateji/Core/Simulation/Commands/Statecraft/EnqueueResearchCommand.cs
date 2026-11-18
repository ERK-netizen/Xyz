using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Teknolojiyi araştırma kuyruğuna ekler.</summary>
    public sealed class EnqueueResearchCommand : GameCommand
    {
        public const string KindKey = "emir.arastirma.kuyrugaEkle";

        public EnqueueResearchCommand(CountryId issuer, TechnologyId technology)
            : base(issuer)
        {
            Technology = technology;
        }

        public TechnologyId Technology { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
