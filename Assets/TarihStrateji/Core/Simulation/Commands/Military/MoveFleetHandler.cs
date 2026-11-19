using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary><see cref="MoveFleetCommand"/> işleyicisi: deniz A* rotası P1'de yeniden hesaplanır.</summary>
    public sealed class MoveFleetHandler : MilitaryHandler<MoveFleetCommand>
    {
        public MoveFleetHandler(GameContent content) : base(content) { }

        public override string Kind => MoveFleetCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, MoveFleetCommand c)
        {
            return FleetRules.CheckMove(War(state), new HexPathfinder(state.World, new NavalMovementCostModel(state.World)), c.Issuer, c.Fleet, c.Target, new List<HexId>());
        }

        protected override void ApplyTyped(CommandContext context, MoveFleetCommand c)
        {
            var path = new List<HexId>();
            GameState state = context.State;
            if (FleetRules.CheckMove(War(state), new HexPathfinder(state.World, new NavalMovementCostModel(state.World)), c.Issuer, c.Fleet, c.Target, path).IsValid)
            {
                FleetRules.SetPath(state.Warfare.Fleets[c.Fleet], path);
            }
        }

        protected override void WriteTyped(MoveFleetCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Fleet);
            w.WriteInt32(c.Target.Column);
            w.WriteInt32(c.Target.Row);
        }

        protected override MoveFleetCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new MoveFleetCommand(issuer, new FleetId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
