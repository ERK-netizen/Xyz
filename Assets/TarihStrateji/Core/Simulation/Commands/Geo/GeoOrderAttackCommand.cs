using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Günlük saldırı emri: düşman birliği veya şehri (P13'te çözülür).</summary>
    public sealed class GeoOrderAttackCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.saldiri";

        public GeoOrderAttackCommand(CountryId issuer, LandUnitId unit, TargetRef target)
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
