using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Intelligence;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// P14 istihbarat (kontrol güncellemesinden sonra): gerçek dünyadan bugünkü görülme listesi
    /// (kara birlikleri — gemidekiler filonun içinde görünmez —, filolar, hava kanatları üslerinde)
    /// ve her ülkenin sensörleri (birlik, filo, kontrol ettiği şehir, görevdeki kanadın hedefi)
    /// kurulur; ekonomisi olan her ülkenin <see cref="ObservedWorld"/>'ü güncellenir.
    /// </summary>
    public sealed class IntelligenceSystem : ISimulationSystem
    {
        private readonly GameContent _content;

        public IntelligenceSystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "istihbarat";

        public SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

        public void Execute(SimulationContext context)
        {
            GameState state = context.State;
            GeoWorld world = state.World;
            WarfareCatalog catalog = _content.Warfare;
            IntelligenceParameters prm = _content.Intelligence;
            var sightings = new List<Sighting>();
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (!u.IsEmbarked)
                {
                    sightings.Add(new Sighting(SightingKind.LandUnit, u.Id.Value, u.Owner, u.Node, u.Strength(catalog.UnitTypes[u.Type])));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                sightings.Add(new Sighting(SightingKind.Fleet, f.Id.Value, f.Owner, f.Node, f.Strength(catalog.ShipTypes[f.Type])));
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                if (world.TryGetCity(w.Base, out City baseCity))
                {
                    sightings.Add(new Sighting(SightingKind.AirWing, w.Id.Value, w.Owner, baseCity.Node, w.Strength(catalog.AirWingTypes[w.Type])));
                }
            }

            GeoWarfareContext war = GeoWarfareContexts.From(state, _content);
            var sensors = new List<Sensor>();
            for (int c = 0; c < state.Economy.Countries.Count; c++)
            {
                CountryId country = state.Economy.Countries.KeyAt(c);
                sensors.Clear();
                CollectSensors(state, war, prm, country, sensors);
                IntelligenceRules.Update(world, prm, state.Intelligence.For(country), sensors, sightings, context.Today);
            }
        }

        private static void CollectSensors(GameState state, GeoWarfareContext war, IntelligenceParameters prm, CountryId country, List<Sensor> into)
        {
            GeoWorld world = state.World;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (u.Owner == country)
                {
                    into.Add(new Sensor(u.Node, prm.LandUnitSight));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                if (f.Owner == country)
                {
                    into.Add(new Sensor(f.Node, prm.FleetSight));
                }
            }

            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (city.Controller == country)
                {
                    into.Add(new Sensor(city.Node, prm.CitySight));
                }
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                if (w.Owner == country && w.Mission != AirMission.None)
                {
                    TransportNodeId target = GeoAirRules.TargetNode(war, w.MissionTarget);
                    if (target.IsValid)
                    {
                        into.Add(new Sensor(target, prm.AirReconSight));
                    }
                }
            }
        }
    }
}
