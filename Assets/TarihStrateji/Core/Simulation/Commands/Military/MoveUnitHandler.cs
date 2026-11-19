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
    /// <summary>Rota emir anında ve P1 uygulamasında aynı kurallarla A* ile hesaplanır.</summary>
    public sealed class MoveUnitHandler : MilitaryHandler<MoveUnitCommand>
    {
        private readonly LandMovementCostModel _movement;

        public MoveUnitHandler(GameContent content) : base(content)
        {
            _movement = new LandMovementCostModel(content.Movement);
        }

        public override string Kind => MoveUnitCommand.KindKey;

        protected override ValidationResult ValidateTyped(GameState state, MoveUnitCommand c)
        {
            return MovementRules.CheckMove(War(state), new HexPathfinder(state.World, _movement), c.Issuer, c.Unit, c.Target, new List<HexId>());
        }

        protected override void ApplyTyped(CommandContext context, MoveUnitCommand c)
        {
            var path = new List<HexId>();
            if (MovementRules.CheckMove(War(context.State), new HexPathfinder(context.State.World, _movement), c.Issuer, c.Unit, c.Target, path).IsValid)
            {
                MovementRules.SetPath(context.State.Warfare.Units[c.Unit], path);
            }
        }

        protected override void WriteTyped(MoveUnitCommand c, CanonicalWriter w)
        {
            w.WriteId(c.Unit);
            w.WriteInt32(c.Target.Column);
            w.WriteInt32(c.Target.Row);
        }

        protected override MoveUnitCommand ReadTyped(CountryId issuer, CanonicalReader r) =>
            new MoveUnitCommand(issuer, new LandUnitId(r.ReadUInt64()), new HexCoord(r.ReadInt32(), r.ReadInt32()));
    }
}
