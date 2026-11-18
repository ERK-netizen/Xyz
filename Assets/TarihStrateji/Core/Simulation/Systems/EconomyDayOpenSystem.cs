using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P0: günlük bütçe defterlerini sıfırlar (P1 emir harcamaları bugünün defterine yazılır).</summary>
    public sealed class EconomyDayOpenSystem : ISimulationSystem
    {
        public string Name => "ekonomi.gunAcilisi";

        public SimulationPhase Phase => SimulationPhase.P0DayOpen;

        public void Execute(SimulationContext context)
        {
            BudgetRules.ResetLedgers(context.State.Economy);
        }
    }
}
