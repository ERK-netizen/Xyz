using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>
    /// P11: ülke başına ikmal ağı (ana şehirler + konvoy limanları; ülke kimliği sırasıyla), ardından
    /// birliklerin ikmal etkisi ve hava kanatlarının günlük bakımı.
    /// </summary>
    public sealed class GeoSupplySystem : GeoMilitarySystem
    {
        public GeoSupplySystem(GameContent content, GeoSupplyCache supply) : base(content, supply) { }

        public override string Name => GeoMilitaryDaySystems.Supply;

        public override SimulationPhase Phase => SimulationPhase.P11Logistics;

        protected override void Run(SimulationContext context, GeoWarfareContext war)
        {
            Supply.Networks.Clear();
            var cost = new EdgeKindCostModel(war.Geo.LandCostPerKm, false, war.Today);
            var access = new ControlledRegionSupplyAccess(war.World);
            for (int i = 0; i < context.State.Economy.Countries.Count; i++)
            {
                CountryId country = context.State.Economy.Countries.KeyAt(i);
                List<GeoSupplySource> sources = GeoSupplyRules.HomeSources(war.World, country);
                GeoConvoyRules.CollectSources(war, country, sources);
                Supply.Networks[country] = GeoSupplyRules.Compute(war.World, cost, access, country, sources, war.Geo.SupplyRange);
            }

            GeoSustainmentRules.ApplyDay(war, Supply.Networks, Notes);
            GeoAirRules.ApplyDay(war);
        }
    }
}
