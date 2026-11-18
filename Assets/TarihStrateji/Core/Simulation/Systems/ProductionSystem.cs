using System;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P8: tesis üretimi (girdi → çıktı, ortak envanter).</summary>
    public sealed class ProductionSystem : ISimulationSystem
    {
        private readonly EconomyCatalog _catalog;

        public ProductionSystem(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "ekonomi.uretim";

        public SimulationPhase Phase => SimulationPhase.P8Production;

        public void Execute(SimulationContext context)
        {
            ProductionRules.RunDay(context.State.Economy, _catalog, context.World, context.Effects, context.Today);
        }
    }
}
