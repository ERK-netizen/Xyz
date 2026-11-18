using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Simulation.Commands.Statecraft
{
    /// <summary>Güncel araştırma projesini seçer (önceki projenin ilerlemesi korunur).</summary>
    public sealed class SetResearchCommand : GameCommand
    {
        public const string KindKey = "emir.arastirma.projeSec";

        public SetResearchCommand(CountryId issuer, TechnologyId technology)
            : base(issuer)
        {
            Technology = technology;
        }

        public TechnologyId Technology { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
