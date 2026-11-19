using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P11 lojistik: kara birliği olan her ülke için ikmal ızgarası (ana şehirler + konvoy limanları,
    /// <see cref="ConvoyRules"/>), ardından kara birliklerinin bakımı, limandaki filoların ve
    /// hava kanatlarının günlük bakımı.
    /// </summary>
    public sealed class SupplySystem : MilitarySystem
    {
        private readonly LandMovementCostModel _movement;

        public SupplySystem(GameContent content) : base(content)
        {
            _movement = new LandMovementCostModel(content.Movement);
        }

        public override string Name => "lojistik.ikmal";

        public override SimulationPhase Phase => SimulationPhase.P11Logistics;

        protected override void Run(SimulationContext context, WarfareContext war)
        {
            var grids = new StableIdMap<CountryId, SupplyGrid>();
            var sources = new List<SupplySource>();
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                CountryId country = war.Warfare.Units.ValueAt(i).Owner;
                if (grids.ContainsKey(country))
                {
                    continue;
                }

                sources.Clear();
                ConvoyRules.CollectSources(war, country, sources);
                var targets = new[] { EffectTarget.Global, EffectTarget.Of(country) };
                Fixed64 range = context.Effects.Evaluate(targets, LogisticsStatKeys.SupplyRange, Content.Logistics.BaseSupplyRange, context.Today);
                grids.Add(country, SupplyRules.Compute(context.World, _movement, OwnTerritorySupplyAccess.Instance, country, sources, range));
            }

            SustainmentRules.ApplyDay(war, grids, Notes);
            FleetRules.ApplyPortDay(war);
            AirRules.ApplyDay(war);
        }
    }
}
