using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Rules;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// Barış/teslim sonrası yerleşim: savaşta olmadığı ve müttefiki olmadığı bir ülkenin
    /// kontrolündeki karada duran kara birliği, kendi (sahip + kontrol) şehirlerinin en yakınına
    /// (büyük çember km, eşitlikte küçük <see cref="CityId"/>) çekilir; şehri yoksa terhis edilir
    /// (asker eve, teçhizat envantere). Yabancı limanda demirli filo denize çıkar. Hava üsleri P14'te
    /// <see cref="AirRules.UpdateBases"/> ile düzelir.
    /// </summary>
    public static class PeaceSettlement
    {
        public const string WithdrawnNote = "bilgi.diplomasi.birlikGeriCekildi";

        public static void Relocate(WarfareContext war, DiplomacyState diplomacy, List<string> notes)
        {
            WorldMap map = war.World;
            var moved = new List<LandUnit>();
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                LandUnit u = war.Warfare.Units.ValueAt(i);
                if (!u.IsEmbarked && Trespassing(war, diplomacy, u.Owner, map.Cell(u.Hex).Controller))
                {
                    moved.Add(u);
                }
            }

            for (int i = 0; i < moved.Count; i++)
            {
                LandUnit u = moved[i];
                City home = NearestHomeCity(map, u.Owner, map.Grid.FromId(u.Hex));
                if (home == null)
                {
                    UnitRules.Remove(war, u, true);
                }
                else
                {
                    MovementRules.Redeploy(u, home.Hex);
                }

                notes.Add(WithdrawnNote + " @ " + u.Id.Value.ToString(CultureInfo.InvariantCulture));
            }

            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                FleetRules.EvictFromPort(war, city, map.Cell(city.Hex).Controller, notes);
            }
        }

        private static bool Trespassing(WarfareContext war, DiplomacyState diplomacy, CountryId owner, CountryId controller)
        {
            return controller.IsValid && controller != owner && !diplomacy.AtWar(owner, controller)
                && !diplomacy.HasTreaty(TreatyKind.Alliance, owner, controller, war.Today);
        }

        private static City NearestHomeCity(WorldMap map, CountryId country, World.Grid.HexCoord from)
        {
            City best = null;
            Fixed64 bestKm = Fixed64.MaxValue;
            for (int c = 0; c < map.Cities.Count; c++)
            {
                City city = map.Cities.ValueAt(c);
                HexCell cell = map.Cell(city.Hex);
                if (cell.Owner != country || cell.Controller != country)
                {
                    continue;
                }

                Fixed64 km = map.Distances.GreatCircleKm(from, cell.Coord);
                if (km < bestKm)
                {
                    bestKm = km;
                    best = city;
                }
            }

            return best;
        }
    }
}
