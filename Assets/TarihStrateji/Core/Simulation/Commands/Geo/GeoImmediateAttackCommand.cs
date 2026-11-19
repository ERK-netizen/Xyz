using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Gün içi taktik saldırı: AP harcar, hemen çözülür, P13'te tekrar çözülmez.</summary>
    public sealed class GeoImmediateAttackCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.aniSaldiri";

        public GeoImmediateAttackCommand(CountryId issuer, LandUnitId unit, TargetRef target)
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
