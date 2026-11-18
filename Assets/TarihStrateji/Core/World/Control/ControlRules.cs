using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Control
{
    /// <summary>
    /// Fiili kontrolün türetilme kuralları (CLAUDE.md §2.3). Sahiplik asla buradan değişmez.
    /// </summary>
    public static class ControlRules
    {
        /// <summary>
        /// Bölge kontrolcüsü = bölge hücrelerinin en çoğunu kontrol eden ülke (kontrolsüz
        /// hücreler <see cref="CountryId.None"/> adayıdır). Eşitlikte mevcut kontrolcü
        /// eşitlerdeyse korunur; değilse en küçük kimlik kazanır (deterministik).
        /// </summary>
        public static CountryId DeriveRegionController(WorldMap map, StrategicRegion region)
        {
            if (region.IsSea || region.Hexes.Count == 0)
            {
                return CountryId.None;
            }

            var counts = new StableIdMap<CountryId, int>(4);
            int noneCount = 0;
            for (int i = 0; i < region.Hexes.Count; i++)
            {
                CountryId controller = map.Cell(region.Hexes[i]).Controller;
                if (!controller.IsValid)
                {
                    noneCount++;
                }
                else
                {
                    counts.TryGetValue(controller, out int current);
                    counts[controller] = current + 1;
                }
            }

            int best = noneCount;
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts.ValueAt(i) > best)
                {
                    best = counts.ValueAt(i);
                }
            }

            int currentCount = region.Controller.IsValid
                ? (counts.TryGetValue(region.Controller, out int c) ? c : 0)
                : noneCount;
            if (currentCount == best)
            {
                return region.Controller;
            }

            if (noneCount == best)
            {
                return CountryId.None;
            }

            for (int i = 0; i < counts.Count; i++)
            {
                if (counts.ValueAt(i) == best)
                {
                    return counts.KeyAt(i);
                }
            }

            return CountryId.None;
        }
    }
}
