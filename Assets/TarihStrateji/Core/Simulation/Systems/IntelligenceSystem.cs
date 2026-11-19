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
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;

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
        private int[] _scratch = new int[0];

        public IntelligenceSystem(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "istihbarat";

        public SimulationPhase Phase => SimulationPhase.P14ControlAndIntelligence;

        public void Execute(SimulationContext context)
        {
            GameState state = context.State;
            WorldMap map = state.World;
            if (_scratch.Length != map.CellCount)
            {
                _scratch = new int[map.CellCount];
            }

            WarfareCatalog catalog = _content.Warfare;
            IntelligenceParameters prm = _content.Intelligence;
            var sightings = new List<Sighting>();
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (!u.IsEmbarked)
                {
                    sightings.Add(new Sighting(SightingKind.LandUnit, u.Id.Value, u.Owner, u.Hex, u.Strength(catalog.UnitTypes[u.Type])));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                sightings.Add(new Sighting(SightingKind.Fleet, f.Id.Value, f.Owner, f.Hex, f.Strength(catalog.ShipTypes[f.Type])));
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                sightings.Add(new Sighting(SightingKind.AirWing, w.Id.Value, w.Owner, map.Cities[w.Base].Hex, w.Strength(catalog.AirWingTypes[w.Type])));
            }

            var sensors = new List<Sensor>();
            for (int c = 0; c < state.Economy.Countries.Count; c++)
            {
                CountryId country = state.Economy.Countries.KeyAt(c);
                sensors.Clear();
                CollectSensors(state, prm, country, sensors);
                IntelligenceRules.Update(map, prm, state.Intelligence.For(country), sensors, sightings, context.Today, _scratch);
            }
        }

        private static void CollectSensors(GameState state, IntelligenceParameters prm, CountryId country, List<Sensor> into)
        {
            WorldMap map = state.World;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (u.Owner == country)
                {
                    into.Add(new Sensor(map.Grid.FromId(u.Hex), prm.LandUnitSight));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                if (f.Owner == country)
                {
                    into.Add(new Sensor(map.Grid.FromId(f.Hex), prm.FleetSight));
                }
            }

            for (int i = 0; i < map.Cities.Count; i++)
            {
                City city = map.Cities.ValueAt(i);
                if (map.Cell(city.Hex).Controller == country)
                {
                    into.Add(new Sensor(map.Grid.FromId(city.Hex), prm.CitySight));
                }
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                if (w.Owner == country && w.Mission != AirMission.None)
                {
                    into.Add(new Sensor(map.Grid.FromId(w.MissionTarget), prm.AirReconSight));
                }
            }
        }
    }
}
