using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Sahip olunan hava üssünde kanat kurar (uçaklar envanterden, pilotlar bölgeden).</summary>
    public sealed class RecruitAirWingCommand : GameCommand
    {
        public const string KindKey = "emir.hava.kanatKur";

        public RecruitAirWingCommand(CountryId issuer, UnitTypeId wingType, HexCoord at)
            : base(issuer)
        {
            WingType = wingType;
            At = at;
        }

        public UnitTypeId WingType { get; }

        public HexCoord At { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
