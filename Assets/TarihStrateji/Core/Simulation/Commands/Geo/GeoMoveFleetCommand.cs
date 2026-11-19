using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Filoyu deniz yolu grafında hedefe (kendi limanı, dost filo, nokta) gönderir.</summary>
    public sealed class GeoMoveFleetCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.filoHareket";

        public GeoMoveFleetCommand(CountryId issuer, FleetId fleet, TargetRef target)
            : base(issuer)
        {
            Fleet = fleet;
            Target = target;
        }

        public FleetId Fleet { get; }

        public TargetRef Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
