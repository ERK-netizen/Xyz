using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P10: önce dünya pazarı, sonra vergi, bakım ve gıda tüketimi.</summary>
    public sealed class TradeAndBudgetSystem : ISimulationSystem
    {
        private readonly GameContent _content;

        public TradeAndBudgetSystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "ekonomi.ticaretVeButce";

        public SimulationPhase Phase => SimulationPhase.P10TradeAndBudget;

        public void Execute(SimulationContext context)
        {
            MarketRules.ClearDay(context.State.Economy, _content.Economy);
            BudgetRules.SettleDay(context.State.Economy, _content.Economy, context.State.Population, context.World, context.Effects, context.Today);
        }
    }
}
