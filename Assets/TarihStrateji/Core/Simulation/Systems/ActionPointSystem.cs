using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P0: kara birliklerinin aksiyon puanları günlük değere yenilenir.</summary>
    public sealed class ActionPointSystem : MilitarySystem
    {
        public ActionPointSystem(GameContent content) : base(content) { }

        public override string Name => "savas.aksiyonPuani";

        public override SimulationPhase Phase => SimulationPhase.P0DayOpen;

        protected override void Run(SimulationContext context, WarfareContext war) => UnitRules.RefreshActionPoints(war);
    }
}
