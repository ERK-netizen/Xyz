using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// Barış/teslim sonrası yerleşim: savaşta olmadığı ve müttefiki olmadığı bir ülkenin kontrolündeki
    /// bölgede (ya da o bölgeye giden yolda) duran kara birliği, kendi (sahip + kontrol) şehirlerinin
    /// kuş uçuşu en yakınına (eşitlikte küçük <see cref="CityId"/>) yerleşir; şehri yoksa terhis edilir
    /// (asker eve, teçhizat envantere). Yabancı limanda demirli filo en yakın kendi limanına rota alır.
    /// Hava üsleri P14'te <see cref="GeoAirRules.UpdateBases"/> ile düzelir.
    /// </summary>
    public static class PeaceSettlement
    {
        public const string WithdrawnNote = "bilgi.diplomasi.birlikGeriCekildi";

        public static void Relocate(GeoWarfareContext war, DiplomacyState diplomacy, List<string> notes)
        {
            GeoWorld world = war.World;
            var moved = new List<LandUnit>();
            for (int i = 0; i < war.Warfare.Units.Count; i++)
            {
                LandUnit u = war.Warfare.Units.ValueAt(i);
                if (!u.IsEmbarked && Trespassing(war, diplomacy, u.Owner, ControllerAt(world, u.Node)))
                {
                    moved.Add(u);
                }
            }

            for (int i = 0; i < moved.Count; i++)
            {
                LandUnit u = moved[i];
                world.Transport.TryGetNode(u.Node, out TransportNode from);
                City home = NearestHomeCity(world, u.Owner, from.Location);
                if (home == null)
                {
                    GeoCombatRules.Remove(war, u, true);
                }
                else
                {
                    GeoMovementRules.Redeploy(u, home.Node);
                }

                notes.Add(WithdrawnNote + " @ " + u.Id.Value.ToString(CultureInfo.InvariantCulture));
            }

            for (int i = 0; i < war.Warfare.Fleets.Count; i++)
            {
                Fleet f = war.Warfare.Fleets.ValueAt(i);
                City port = GeoUnitQueries.CityAt(war, f.Node);
                if (port != null && f.Route.Count == 0 && Trespassing(war, diplomacy, f.Owner, port.Controller))
                {
                    GeoNavalRules.Withdraw(war, f);
                }
            }
        }

        private static CountryId ControllerAt(GeoWorld world, TransportNodeId node) =>
            world.Transport.TryGetNode(node, out TransportNode n) && world.TryGetControl(n.Region, out RegionControl control)
                ? control.Controller
                : CountryId.None;

        private static bool Trespassing(GeoWarfareContext war, DiplomacyState diplomacy, CountryId owner, CountryId controller)
        {
            return controller.IsValid && controller != owner && !diplomacy.AtWar(owner, controller)
                && !diplomacy.HasTreaty(TreatyKind.Alliance, owner, controller, war.Today);
        }

        private static City NearestHomeCity(GeoWorld world, CountryId country, GeoPoint from)
        {
            City best = null;
            long bestMeters = long.MaxValue;
            for (int c = 0; c < world.Cities.Count; c++)
            {
                world.TryGetCity(world.Cities[c], out City city);
                if (city.Owner != country || city.Controller != country)
                {
                    continue;
                }

                long meters = GeoMath.DistanceMeters(from, city.Location);
                if (meters < bestMeters)
                {
                    bestMeters = meters;
                    best = city;
                }
            }

            return best;
        }
    }
}
