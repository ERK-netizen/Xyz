using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="AssignAirMissionCommand"/> işleyicisi; kural mantığı Warfare modülündedir.</summary>
    public sealed class AssignAirMissionHandler : MilitaryHandler<AssignAirMissionCommand>
    {
        public AssignAirMissionHandler(GameContent content) : base(content) { }

        public override string Kind => AssignAirMissionCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, AssignAirMissionCommand c) => AirRules.CheckMission(War(state), c.Issuer, c.Wing, c.Mission, c.Target);

        protected override void ApplyTyped(CommandContext context, AssignAirMissionCommand c)
        {
            AirRules.SetMission(context.State.Warfare.AirWings[c.Wing], c.Mission, c.Mission == AirMission.None ? HexId.None : context.State.World.Grid.ToId(c.Target));
        }

        protected override void WriteTyped(AssignAirMissionCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Wing);
            w.WriteInt32((int)c.Mission);
            w.WriteInt32(c.Target.Column);
            w.WriteInt32(c.Target.Row);
        }

        protected override AssignAirMissionCommand ReadTyped(CountryId issuer, CanonicalReader r) => new AssignAirMissionCommand(issuer, new AirWingId(r.ReadUInt64()), (AirMission)r.ReadInt32(), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
