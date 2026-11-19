using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Limandaki boş filoyu terhis eder (gemiler envantere döner).</summary>
    public sealed class DisbandFleetCommand : GameCommand
    {
        public const string KindKey = "emir.deniz.filoTerhis";

        public DisbandFleetCommand(CountryId issuer, FleetId fleet)
            : base(issuer)
        {
            Fleet = fleet;
        }

        public FleetId Fleet { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
