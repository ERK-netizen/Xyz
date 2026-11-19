using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Ana limanda filo kurar (gemiler envanterden, mürettebat bölgeden).</summary>
    public sealed class BuildFleetCommand : GameCommand
    {
        public const string KindKey = "emir.deniz.filoKur";

        public BuildFleetCommand(CountryId issuer, UnitTypeId shipType, HexCoord at)
            : base(issuer)
        {
            ShipType = shipType;
            At = at;
        }

        public UnitTypeId ShipType { get; }

        public HexCoord At { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
