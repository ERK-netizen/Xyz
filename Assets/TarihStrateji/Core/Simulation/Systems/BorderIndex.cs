using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// Kara sınırı komşulukları (siyasi haritadan, fiili kontrole göre): kontrol ettikleri kara
    /// hücreleri bitişik olan ülke çiftleri. Tek geçişte O(hücre) hesaplanır; sıralı ve tekrarsızdır.
    /// </summary>
    public static class BorderIndex
    {
        public static void Compute(WorldMap map, List<ulong> pairs)
        {
            pairs.Clear();
            for (int i = 0; i < map.CellCount; i++)
            {
                HexCell cell = map.CellAt(i);
                if (cell.IsWater || !cell.Controller.IsValid || cell.Controller.Value > 0xFFFFFFFFUL)
                {
                    continue;
                }

                // Her bitişiklik iki kez görülür; yalnız doğu yarısı (E, NE, NW) yeter.
                for (int d = 0; d < 3; d++)
                {
                    if (!map.TryGetNeighborCell(cell, (HexDirection)d, out HexCell n) || n.IsWater || !n.Controller.IsValid
                        || n.Controller == cell.Controller || n.Controller.Value > 0xFFFFFFFFUL)
                    {
                        continue;
                    }

                    ulong a = cell.Controller.Value;
                    ulong b = n.Controller.Value;
                    ulong key = a < b ? (a << 32) | b : (b << 32) | a;
                    int at = pairs.BinarySearch(key);
                    if (at < 0)
                    {
                        pairs.Insert(~at, key);
                    }
                }
            }
        }

        public static void NeighborsOf(List<ulong> pairs, CountryId country, List<CountryId> into)
        {
            into.Clear();
            for (int i = 0; i < pairs.Count; i++)
            {
                ulong hi = pairs[i] >> 32;
                ulong lo = pairs[i] & 0xFFFFFFFFUL;
                if (hi == country.Value)
                {
                    into.Add(new CountryId(lo));
                }
                else if (lo == country.Value)
                {
                    into.Add(new CountryId(hi));
                }
            }

            into.Sort();
        }
    }
}
