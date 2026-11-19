using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Gemideki birliğe liman şehrine çıkarma emri (P13'te çözülür).</summary>
    public sealed class GeoLandingCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.cikarma";

        public GeoLandingCommand(CountryId issuer, LandUnitId unit, CityId target)
            : base(issuer)
        {
            Unit = unit;
            Target = target;
        }

        public LandUnitId Unit { get; }

        public CityId Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
