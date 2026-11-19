using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Logistics;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P11: birliği olan her ülke için ikmal ağı (kaynak: kuşatılmamış, merkez sektörü kontrol
    /// edilen şehirler; menzil × ikmal menzili etkileri), ardından yıpranma/toparlanma/takviye.
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
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                CountryId country = war.Warfare.Units.ValueAt(i).Owner;
                if (grids.ContainsKey(country))
                {
                    continue;
                }

                var sources = new List<HexCoord>();
                WorldMap map = context.World;
                for (int c = 0; c < map.Cities.Count; c++)
                {
                    City city = map.Cities.ValueAt(c);
                    if (map.Cell(city.Hex).Controller == country && map.AdministrativeSectorOf(city).Controller == country && !war.Warfare.IsBesieged(city.Id))
                    {
                        sources.Add(map.Grid.FromId(city.Hex));
                    }
                }

                var targets = new[] { EffectTarget.Global, EffectTarget.Of(country) };
                Fixed64 range = context.Effects.Evaluate(targets, LogisticsStatKeys.SupplyRange, Content.Logistics.BaseSupplyRange, context.Today);
                grids.Add(country, SupplyRules.Compute(map, _movement, OwnTerritorySupplyAccess.Instance, country, sources, range));
            }

            SustainmentRules.ApplyDay(war, grids, Notes);
        }
    }
}
