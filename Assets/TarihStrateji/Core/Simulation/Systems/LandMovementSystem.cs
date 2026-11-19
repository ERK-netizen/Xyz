using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P12: kara birlikleri rotalarında hareket bütçesi kadar ilerler (gemidekiler hariç).</summary>
    public sealed class LandMovementSystem : MilitarySystem
    {
        private readonly LandMovementCostModel _movement;

        public LandMovementSystem(GameContent content) : base(content)
        {
            _movement = new LandMovementCostModel(content.Movement);
        }

        public override string Name => "savas.hareket";

        public override SimulationPhase Phase => SimulationPhase.P12Movement;

        protected override void Run(SimulationContext context, WarfareContext war) => MovementRules.AdvanceDay(war, _movement, Notes);
    }
}
