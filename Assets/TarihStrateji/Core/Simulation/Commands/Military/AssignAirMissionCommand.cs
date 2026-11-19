using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Kanada görev ve hedef verir; hedef üssten menzil dışındaysa reddedilir.</summary>
    public sealed class AssignAirMissionCommand : GameCommand
    {
        public const string KindKey = "emir.hava.gorevVer";

        public AssignAirMissionCommand(CountryId issuer, AirWingId wing, AirMission mission, HexCoord target)
            : base(issuer)
        {
            Wing = wing;
            Mission = mission;
            Target = target;
        }

        public AirWingId Wing { get; }

        public AirMission Mission { get; }

        public HexCoord Target { get; }

        public override CommandAuthority Authority => CommandAuthority.Player;

        public override string Kind => KindKey;
    }
}
