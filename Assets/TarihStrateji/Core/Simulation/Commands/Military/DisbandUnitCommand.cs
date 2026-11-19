using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Kendi toprağımızdaki birliği terhis eder.</summary>
    public sealed class DisbandUnitCommand : GameCommand
    {
        public const string KindKey = "emir.savas.birlikTerhis";

        public DisbandUnitCommand(CountryId issuer, LandUnitId unit)
            : base(issuer)
        {
            Unit = unit;
        }

        public LandUnitId Unit { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
