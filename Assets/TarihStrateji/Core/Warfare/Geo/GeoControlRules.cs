using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Logistics.Geo;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Bölge kontrolü (P14, §4.4, MAP-G08): bölgedeki kara birliklerinden baskın taraf bulunur ve
    /// <see cref="RegionControlRules"/> ilerletilir.
    /// <list type="bullet">
    /// <item>bölgede kontrolcü veya ona düşman olmayan birlik varken düşman birlik de varsa çekişmeli (baskın yok);</item>
    /// <item>yalnız kontrolcünün düşmanları varsa: aralarında savaş yoksa en çok askeri olan baskındır, ama bölgede
    /// kontrolcünün elinde şehir kaldıkça baskın sayılmaz (kilit şehir);</item>
    /// <item>baskın, bölgede veya bölgeye tek kara kenarıyla bağlı bir düğümde ikmal alıyorsa ikmallidir.</item>
    /// </list>
    /// Hukukî sahip hiç değişmez; şehirler ayrıca garnizon muharebesiyle alınır.
    /// </summary>
    public static class GeoControlRules
    {
        public const string RegionOccupiedNote = "bilgi.savas.bolgeKontrolDegisti";

        public static void UpdateRegions(GeoWarfareContext ctx, IReadOnlyDictionary<CountryId, GeoSupplyNetwork> supply, List<string> notes)
        {
            IReadOnlyList<RegionId> regions = ctx.World.Atlas.Regions;
            for (int r = 0; r < regions.Count; r++)
            {
                if (!ctx.World.TryGetControl(regions[r], out RegionControl control))
                {
                    continue;
                }

                CountryId dominant = Dominant(ctx, control);
                bool supplied = dominant == control.Controller
                    || (dominant.IsValid && supply.TryGetValue(dominant, out GeoSupplyNetwork net) && Supplied(ctx, net, control.Region));
                if (RegionControlRules.Advance(control, dominant, supplied, ctx.Geo.RegionControl))
                {
                    notes.Add(RegionOccupiedNote + " @ " + control.Region.Value.ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        public static CountryId Dominant(GeoWarfareContext ctx, RegionControl control)
        {
            var manpower = new SortedDictionary<ulong, long>();
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.IsEmbarked || !ctx.World.Transport.TryGetNode(u.Node, out TransportNode node) || node.Region != control.Region)
                {
                    continue;
                }

                manpower.TryGetValue(u.Owner.Value, out long men);
                manpower[u.Owner.Value] = men + u.Manpower;
            }

            if (manpower.Count == 0)
            {
                return control.Controller;
            }

            bool friendly = false;
            bool hostile = false;
            foreach (ulong c in manpower.Keys)
            {
                var country = new CountryId(c);
                if (ctx.IsHostile(country, control.Controller))
                {
                    hostile = true;
                }
                else
                {
                    friendly = true;
                }
            }

            if (!hostile)
            {
                return control.Controller;
            }

            if (friendly || ControllerHoldsCity(ctx, control))
            {
                return CountryId.None;
            }

            CountryId best = CountryId.None;
            long bestMen = -1L;
            foreach (KeyValuePair<ulong, long> entry in manpower)
            {
                var country = new CountryId(entry.Key);
                if (best.IsValid && ctx.IsHostile(best, country))
                {
                    return CountryId.None;
                }

                if (entry.Value > bestMen)
                {
                    best = country;
                    bestMen = entry.Value;
                }
            }

            return best;
        }

        private static bool ControllerHoldsCity(GeoWarfareContext ctx, RegionControl control)
        {
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (city.Region == control.Region && city.Controller == control.Controller)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Supplied(GeoWarfareContext ctx, GeoSupplyNetwork net, RegionId region)
        {
            IReadOnlyList<TransportNodeId> nodes = ctx.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ctx.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                if (node.Region != region)
                {
                    continue;
                }

                if (net.NodeLevel(node.Id).IsPositive)
                {
                    return true;
                }

                IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(node.Id);
                for (int e = 0; e < edges.Count; e++)
                {
                    if (!edges[e].IsNaval && net.NodeLevel(edges[e].Other(node.Id)).IsPositive)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
