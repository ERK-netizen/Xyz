using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>P0: kara birliklerinin aksiyon puanları günlük değere yenilenir.</summary>
    public sealed class GeoActionPointSystem : GeoMilitarySystem
    {
        public GeoActionPointSystem(GameContent content, GeoSupplyCache supply) : base(content, supply) { }

        public override string Name => GeoMilitaryDaySystems.ActionPoints;

        public override SimulationPhase Phase => SimulationPhase.P0DayOpen;

        protected override void Run(SimulationContext context, GeoWarfareContext war) => GeoSustainmentRules.RefreshActionPoints(war);
    }
}
