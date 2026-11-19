using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Logistics.Geo
{
    /// <summary>
    /// Ulaşım grafında ikmal (P11, §5.6): kaynaklardan çok kaynaklı Dijkstra; kenar maliyeti ulaşım
    /// maliyet modelinden (demiryolu ikmali uzağa taşır, açılmamış hat kullanılmaz). Düzey = 1 − maliyet /
    /// menzil; seviyesi l olan kaynak menzilin (1 − l)'si harcanmış gibi başlar. Yalnız erişimin izin
    /// verdiği düğümlerden geçer: düşman kontrolündeki bölge ikmali keser. Eşit maliyette küçük düğüm
    /// kimliği önce açılır; sonuç determinist.
    /// </summary>
    public static class GeoSupplyRules
    {
        /// <summary>Kaynaklar: ülkenin sahip + kontrol ettiği, kuşatılmamış şehirleri (seviye 1), kimlik sırasıyla.</summary>
        public static List<GeoSupplySource> HomeSources(GeoWorld world, CountryId country)
        {
            var sources = new List<GeoSupplySource>();
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (city.Owner == country && city.Controller == country && city.Siege != SiegeState.Besieged)
                {
                    sources.Add(new GeoSupplySource(city.Node, Fixed64.One));
                }
            }

            return sources;
        }

        public static GeoSupplyNetwork Compute(
            GeoWorld world,
            ITransportCostModel cost,
            IGeoSupplyAccess access,
            CountryId country,
            IReadOnlyList<GeoSupplySource> sources,
            Fixed64 range)
        {
            if (world == null || cost == null || access == null || sources == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var best = new Dictionary<TransportNodeId, long>();
            var done = new HashSet<TransportNodeId>();
            var open = new SortedSet<(long Cost, ulong Node)>();
            var nodeLevels = new Dictionary<TransportNodeId, Fixed64>();
            var regionLevels = new Dictionary<RegionId, Fixed64>();
            if (!range.IsPositive)
            {
                return new GeoSupplyNetwork(country, nodeLevels, regionLevels);
            }

            for (int s = 0; s < sources.Count; s++)
            {
                Fixed64 level = Fixed64.Clamp01(sources[s].Level);
                if (!level.IsPositive || !world.Transport.TryGetNode(sources[s].Node, out TransportNode node) || !access.CanPass(country, node))
                {
                    continue;
                }

                Relax(best, open, node.Id, (range * (Fixed64.One - level)).Raw);
            }

            while (open.Count > 0)
            {
                (long spent, ulong raw) = open.Min;
                open.Remove(open.Min);
                var id = new TransportNodeId(raw);
                if (!done.Add(id))
                {
                    continue;
                }

                world.Transport.TryGetNode(id, out TransportNode from);
                Fixed64 nodeLevel = Fixed64.One - Fixed64.MulDiv(Fixed64.FromRaw(spent), Fixed64.One, range);
                nodeLevels[id] = nodeLevel;
                if (from.Region.IsValid && (!regionLevels.TryGetValue(from.Region, out Fixed64 regionBest) || nodeLevel > regionBest))
                {
                    regionLevels[from.Region] = nodeLevel;
                }

                IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(id);
                for (int e = 0; e < edges.Count; e++)
                {
                    TransportNodeId nextId = edges[e].Other(id);
                    world.Transport.TryGetNode(nextId, out TransportNode next);
                    if (done.Contains(nextId) || !access.CanPass(country, next) || !cost.TryGetCost(edges[e], from, next, out long step))
                    {
                        continue;
                    }

                    // Maliyet birimi menzille aynıdır (ham Fixed64 = maliyet × 10⁶).
                    long total = checked(spent + (step * Fixed64.Scale));
                    if (total < range.Raw)
                    {
                        Relax(best, open, nextId, total);
                    }
                }
            }

            return new GeoSupplyNetwork(country, nodeLevels, regionLevels);
        }

        private static void Relax(Dictionary<TransportNodeId, long> best, SortedSet<(long, ulong)> open, TransportNodeId node, long cost)
        {
            if (best.TryGetValue(node, out long known))
            {
                if (cost >= known)
                {
                    return;
                }

                open.Remove((known, node.Value));
            }

            best[node] = cost;
            open.Add((cost, node.Value));
        }
    }
}
