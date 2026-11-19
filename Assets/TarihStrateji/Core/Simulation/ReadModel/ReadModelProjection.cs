using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Simulation.ReadModel
{
    /// <summary>
    /// <see cref="GameState"/>'ten sunum görünümleri (Domain türleri). Savaş sisi burada uygulanır:
    /// oyuncu kendi birliklerini ve kendi <see cref="ObservedWorld"/> temaslarını görür; Gözlemci
    /// modunda (oyuncu ülkesi yok) her şey görünür ve güçler kesindir.
    /// </summary>
    public static class ReadModelProjection
    {
        public static CellView Cell(GameState state, int index)
        {
            HexCell c = state.World.CellAt(index);
            return new CellView((byte)c.Terrain, c.IsWater, c.Owner, c.Controller, c.City);
        }

        public static bool IsVisible(GameState state, int index)
        {
            if (!state.PlayerCountry.IsValid)
            {
                return true;
            }

            return state.Intelligence.Countries.TryGetValue(state.PlayerCountry, out ObservedWorld view) && view.IsVisible(index);
        }

        public static void Cities(GameState state, List<CityView> into)
        {
            WorldMap map = state.World;
            for (int i = 0; i < map.Cities.Count; i++)
            {
                City city = map.Cities.ValueAt(i);
                HexCell cell = map.Cell(city.Hex);
                into.Add(new CityView(city.Id, map.Grid.IndexOf(cell.Coord), city.NameKey, (byte)city.Tier, cell.Owner, cell.Controller, state.Warfare.IsBesieged(city.Id)));
            }
        }

        public static bool TryCountry(GameState state, CountryId country, out CountryView view)
        {
            if (!state.Economy.Countries.TryGetValue(country, out CountryEconomy economy))
            {
                view = default;
                return false;
            }

            Fixed64 stability = state.Politics.Countries.TryGetValue(country, out CountryPolitics politics) ? politics.Stability : Fixed64.One;
            long population = 0L;
            for (int i = 0; i < state.World.Regions.Count; i++)
            {
                StrategicRegion region = state.World.Regions.ValueAt(i);
                if (region.Owner == country && state.Population.Regions.TryGetValue(region.Id, out RegionPopulation p))
                {
                    population += p.Total;
                }
            }

            int divisions = 0;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                if (state.Warfare.Units.ValueAt(i).Owner == country)
                {
                    divisions++;
                }
            }

            view = new CountryView(country, economy.Treasury, economy.TaxRate, stability, population, divisions, state.Diplomacy.IsAtAnyWar(country));
            return true;
        }

        public static void Units(GameState state, GameContent content, List<UnitView> into)
        {
            CountryId player = state.PlayerCountry;
            bool observer = !player.IsValid;
            WorldMap map = state.World;
            for (int i = 0; i < state.Warfare.Units.Count; i++)
            {
                LandUnit u = state.Warfare.Units.ValueAt(i);
                if (observer || u.Owner == player)
                {
                    into.Add(new UnitView(u.Id.Value, UnitViewKind.Land, u.Owner, Index(map, u.Hex), u.Strength(content.Warfare.UnitTypes[u.Type]), true, u.Owner == player));
                }
            }

            for (int i = 0; i < state.Warfare.Fleets.Count; i++)
            {
                Fleet f = state.Warfare.Fleets.ValueAt(i);
                if (observer || f.Owner == player)
                {
                    into.Add(new UnitView(f.Id.Value, UnitViewKind.Fleet, f.Owner, Index(map, f.Hex), f.Strength(content.Warfare.ShipTypes[f.Type]), true, f.Owner == player));
                }
            }

            for (int i = 0; i < state.Warfare.AirWings.Count; i++)
            {
                AirWing w = state.Warfare.AirWings.ValueAt(i);
                if (observer || w.Owner == player)
                {
                    into.Add(new UnitView(w.Id.Value, UnitViewKind.AirWing, w.Owner, Index(map, map.Cities[w.Base].Hex), w.Strength(content.Warfare.AirWingTypes[w.Type]), true, w.Owner == player));
                }
            }

            if (observer || !state.Intelligence.Countries.TryGetValue(player, out ObservedWorld view))
            {
                return;
            }

            for (int i = 0; i < view.Contacts.Count; i++)
            {
                ContactReport c = view.Contacts[i];
                UnitViewKind kind = c.Kind == SightingKind.Fleet ? UnitViewKind.Fleet : c.Kind == SightingKind.AirWing ? UnitViewKind.AirWing : UnitViewKind.Land;
                into.Add(new UnitView(c.EntityId, kind, c.Owner, Index(map, c.LastHex), c.EstimatedStrength, c.IsExact, false));
            }
        }

        private static int Index(WorldMap map, HexId hex) => map.Grid.IndexOf(map.Grid.FromId(hex));
    }
}
