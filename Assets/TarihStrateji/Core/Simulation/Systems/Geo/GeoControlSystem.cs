using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>P14: abluka → kuşatma → bölge kontrolü (P11 ikmal ağıyla) → hava üsleri.</summary>
    public sealed class GeoControlSystem : GeoMilitarySystem
    {
        public GeoControlSystem(GameContent content, GeoSupplyCache supply) : base(content, supply) { }

        public override string Name => GeoMilitaryDaySystems.Control;

        public override SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

        protected override void Run(SimulationContext context, GeoWarfareContext war)
        {
            GeoNavalRules.UpdateBlockades(war);
            GeoSiegeRules.Update(war);
            GeoControlRules.UpdateRegions(war, Supply.Networks, Notes);
            GeoAirRules.UpdateBases(war, Notes);
        }
    }
}
