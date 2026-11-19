using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Filoya deniz rotası verir (hedef deniz hücresi veya kendi kullanılabilir limanı).</summary>
    public sealed class MoveFleetCommand : GameCommand
    {
        public const string KindKey = "emir.deniz.filoHareket";

        public MoveFleetCommand(CountryId issuer, FleetId fleet, HexCoord target)
            : base(issuer)
        {
            Fleet = fleet;
            Target = target;
        }

        public FleetId Fleet { get; }

        public HexCoord Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
