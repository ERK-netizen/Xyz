using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P12: filolar seyreder (düşman filosuyla temasta durur); taşınan birlikler filoyla gider.</summary>
    public sealed class NavalMovementSystem : MilitarySystem
    {
        public NavalMovementSystem(GameContent content) : base(content) { }

        public override string Name => "deniz.hareket";

        public override SimulationPhase Phase => SimulationPhase.P12Movement;

        protected override void Run(SimulationContext context, WarfareContext war) =>
            FleetRules.AdvanceDay(war, new NavalMovementCostModel(context.World), Notes);
    }
}
