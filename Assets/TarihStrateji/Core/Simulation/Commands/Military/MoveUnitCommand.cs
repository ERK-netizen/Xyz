using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Birliği hedef hücreye yürütür (rota A* ile).</summary>
    public sealed class MoveUnitCommand : GameCommand
    {
        public const string KindKey = "emir.savas.hareket";

        public MoveUnitCommand(CountryId issuer, LandUnitId unit, HexCoord target)
            : base(issuer)
        {
            Unit = unit;
            Target = target;
        }

        public LandUnitId Unit { get; }

        public HexCoord Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
