using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Birliğin hareket ve saldırı emirlerini kaldırır.</summary>
    public sealed class HoldPositionCommand : GameCommand
    {
        public const string KindKey = "emir.savas.mevziTut";

        public HoldPositionCommand(CountryId issuer, LandUnitId unit)
            : base(issuer)
        {
            Unit = unit;
        }

        public LandUnitId Unit { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
