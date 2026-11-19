using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Limandaki birliği aynı limandaki nakliye filosuna bindirir (kapasite > 0).</summary>
    public sealed class GeoEmbarkCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.gemiyeBin";

        public GeoEmbarkCommand(CountryId issuer, LandUnitId unit, FleetId fleet)
            : base(issuer)
        {
            Unit = unit;
            Fleet = fleet;
        }

        public LandUnitId Unit { get; }

        public FleetId Fleet { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
