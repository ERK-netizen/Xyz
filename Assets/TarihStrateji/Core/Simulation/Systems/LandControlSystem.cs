using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P14: işgal, abluka, kuşatma (ablukaya bağlı) ve düşen hava üslerinin boşaltılması.</summary>
    public sealed class LandControlSystem : MilitarySystem
    {
        public LandControlSystem(GameContent content) : base(content) { }

        public override string Name => "savas.kontrol";

        public override SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            WarfareControlRules.Occupy(war);
            WarfareControlRules.UpdateBlockades(war);
            WarfareControlRules.UpdateSieges(war);
            AirRules.UpdateBases(war, Notes);
        }
    }
}
