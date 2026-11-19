using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>
    /// İkmal ağı (P11): kaynaklardan (kuşatılmamış, kontrol edilen şehirler) çok kaynaklı
    /// Dijkstra; kenar maliyeti hareket modelinin maliyetidir (demiryolu ikmali uzağa taşır).
    /// Düzey = 1 − maliyet / menzil, [0, 1]. Yalnız <see cref="ISupplyAccess"/>'in izin verdiği
    /// hücrelerden geçer: düşman hattı ikmali keser, kuşatılmış şehir kaynak olamaz.
    /// </summary>
    public static class SupplyRules
    {
        public static SupplyGrid Compute(
            WorldMap map,
            IMovementCostModel movement,
            ISupplyAccess access,
            CountryId country,
            IReadOnlyList<HexCoord> sources,
            Fixed64 range)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var full = new SupplySource[sources.Count];
            for (int i = 0; i < full.Length; i++)
            {
                full[i] = new SupplySource(sources[i], Fixed64.One);
            }

            return Compute(map, movement, access, country, full, range);
        }

        /// <summary>
        /// Seviyeli kaynaklarla hesap: seviyesi <c>l</c> olan kaynak, menzilin <c>(1 − l)</c> kadarı
        /// harcanmış gibi başlar (ör. konvoyla beslenen liman). Tam seviyeli kaynaklarla sonuç
        /// diğer aşırı yüklemeyle birebir aynıdır.
        /// </summary>
        public static SupplyGrid Compute(
            WorldMap map,
            IMovementCostModel movement,
            ISupplyAccess access,
            CountryId country,
            IReadOnlyList<SupplySource> sources,
            Fixed64 range)
        {
            if (map == null || movement == null || access == null || sources == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            int n = map.CellCount;
            var cost = new long[n];
            var done = new bool[n];
            var level = new long[n];
            for (int i = 0; i < n; i++)
            {
                cost[i] = long.MaxValue;
            }

            if (!range.IsPositive)
            {
                return new SupplyGrid(country, level);
            }

            var open = new SortedSet<(long Cost, int Index)>();
            for (int s = 0; s < sources.Count; s++)
            {
                int index = map.Grid.IndexOf(sources[s].Coord);
                Fixed64 sourceLevel = Fixed64.Clamp01(sources[s].Level);
                if (!sourceLevel.IsPositive || !access.CanPass(country, map.CellAt(index)))
                {
                    continue;
                }

                long start = (range * (Fixed64.One - sourceLevel)).Raw;
                if (start < cost[index])
                {
                    if (cost[index] != long.MaxValue)
                    {
                        open.Remove((cost[index], index));
                    }

                    cost[index] = start;
                    open.Add((start, index));
                }
            }

            HexGrid grid = map.Grid;
            while (open.Count > 0)
            {
                (long current, int index) = open.Min;
                open.Remove(open.Min);
                if (done[index])
                {
                    continue;
                }

                done[index] = true;
                level[index] = Fixed64.Max(Fixed64.Zero, Fixed64.One - Fixed64.MulDiv(Fixed64.FromRaw(current), Fixed64.One, range)).Raw;
                HexCell cell = map.CellAt(index);
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    if (!grid.TryGetNeighbor(cell.Coord, (HexDirection)d, out HexCoord n2))
                    {
                        continue;
                    }

                    int next = grid.IndexOf(n2);
                    HexCell nextCell = map.CellAt(next);
                    if (done[next] || !access.CanPass(country, nextCell))
                    {
                        continue;
                    }

                    map.Distances.TryGetNeighborKm(cell.Coord, (HexDirection)d, out Fixed64 km);
                    if (!movement.TryGetStepCost(cell, nextCell, km, out Fixed64 step))
                    {
                        continue;
                    }

                    long total = current + step.Raw;
                    if (total <= range.Raw && total < cost[next])
                    {
                        if (cost[next] != long.MaxValue)
                        {
                            open.Remove((cost[next], next));
                        }

                        cost[next] = total;
                        open.Add((total, next));
                    }
                }
            }

            return new SupplyGrid(country, level);
        }
    }
}
