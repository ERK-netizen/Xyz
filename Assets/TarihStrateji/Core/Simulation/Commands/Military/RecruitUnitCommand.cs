using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Sahip ve kontrol ettiğimiz bir şehirde birlik toplar (asker + ekipman).</summary>
    public sealed class RecruitUnitCommand : GameCommand
    {
        public const string KindKey = "emir.savas.birlikTopla";

        public RecruitUnitCommand(CountryId issuer, UnitTypeId unitType, HexCoord at)
            : base(issuer)
        {
            UnitType = unitType;
            At = at;
        }

        public UnitTypeId UnitType { get; }

        public HexCoord At { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
