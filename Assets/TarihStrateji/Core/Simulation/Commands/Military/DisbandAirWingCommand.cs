using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Kanadı terhis eder (uçaklar envantere, pilotlar bölgeye döner).</summary>
    public sealed class DisbandAirWingCommand : GameCommand
    {
        public const string KindKey = "emir.hava.kanatTerhis";

        public DisbandAirWingCommand(CountryId issuer, AirWingId wing)
            : base(issuer)
        {
            Wing = wing;
        }

        public AirWingId Wing { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
