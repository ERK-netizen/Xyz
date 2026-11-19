using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>Hava kanadına görev verir; hedef üsten menzil içinde olmalı.</summary>
    public sealed class GeoAirMissionCommand : GameCommand
    {
        public const string KindKey = "emir.cografya.havaGorevi";

        public GeoAirMissionCommand(CountryId issuer, AirWingId wing, AirMission mission, TargetRef target)
            : base(issuer)
        {
            Wing = wing;
            Mission = mission;
            Target = target;
        }

        public AirWingId Wing { get; }

        public AirMission Mission { get; }

        public TargetRef Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
