using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P7: iş gücü ataması (seferberlik iş gücünü azaltır).</summary>
    public sealed class WorkforceSystem : ISimulationSystem
    {
        private readonly GameContent _content;

        public WorkforceSystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "ekonomi.isGucu";

        public SimulationPhase Phase => SimulationPhase.P7Workforce;

        public void Execute(SimulationContext context)
        {
            WorkforceRules.AssignDay(context.State.Economy, context.State.Population, context.World, _content.Economy, _content.Demography);
        }
    }
}
