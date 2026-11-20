using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>
    /// Kara sınırı komşulukları (siyasi haritadan, fiilî kontrole göre): kara geçişine açık sınırla
    /// (<see cref="BorderGraph"/>) bitişik bölgelerin kontrolcüsü farklı olan ülke çiftleri. Bölge
    /// kimliği sırasıyla tek geçişte hesaplanır; sonuç sıralı ve tekrarsızdır.
    /// </summary>
    public static class BorderIndex
    {
        public static void Compute(GeoWorld world, List<ulong> pairs)
        {
            pairs.Clear();
            for (int i = 0; i < world.Regions.Count; i++)
            {
                world.TryGetControl(world.Regions[i], out RegionControl region);
                if (!region.Controller.IsValid || region.Controller.Value > 0xFFFFFFFFUL)
                {
                    continue;
                }

                IReadOnlyList<BorderLink> links = world.Borders.Neighbors(region.Region);
                for (int l = 0; l < links.Count; l++)
                {
                    if (!links[l].LandPassable || links[l].Neighbor < region.Region
                        || !world.TryGetControl(links[l].Neighbor, out RegionControl other) || !other.Controller.IsValid
                        || other.Controller == region.Controller || other.Controller.Value > 0xFFFFFFFFUL)
                    {
                        continue;
                    }

                    ulong a = region.Controller.Value;
                    ulong b = other.Controller.Value;
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
