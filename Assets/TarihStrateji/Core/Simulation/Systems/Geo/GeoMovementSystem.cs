using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>P12: filolar (taşıdıkları birliklerle) ve kara birlikleri rotaları boyunca ilerler.</summary>
    public sealed class GeoMovementSystem : GeoMilitarySystem
    {
        public GeoMovementSystem(GameContent content, GeoSupplyCache supply) : base(content, supply) { }

        public override string Name => GeoMilitaryDaySystems.Movement;

        public override SimulationPhase Phase => SimulationPhase.P12Movement;

        protected override void Run(SimulationContext context, GeoWarfareContext war)
        {
            GeoNavalRules.AdvanceDay(war, Notes);
            GeoMovementRules.AdvanceDay(war, Notes);
        }
    }
}
