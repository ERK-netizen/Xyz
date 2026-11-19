using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Kara birliğini hedefe (şehir, bölge, nokta, dost birlik) yürütür; rota ulaşım grafında A*.</summary>
    public sealed class GeoMoveUnitCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.karaHareket";

        public GeoMoveUnitCommand(CountryId issuer, LandUnitId unit, TargetRef target)
            : base(issuer)
        {
            Unit = unit;
            Target = target;
        }

        public LandUnitId Unit { get; }

        public TargetRef Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
