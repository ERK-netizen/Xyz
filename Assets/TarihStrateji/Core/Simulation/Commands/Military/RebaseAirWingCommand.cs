using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Kanadı başka bir hava üssüne taşır; görevi sona erer.</summary>
    public sealed class RebaseAirWingCommand : GameCommand
    {
        public const string KindKey = "emir.hava.usDegistir";

        public RebaseAirWingCommand(CountryId issuer, AirWingId wing, HexCoord at)
            : base(issuer)
        {
            Wing = wing;
            At = at;
        }

        public AirWingId Wing { get; }

        public HexCoord At { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
