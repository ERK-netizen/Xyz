using System.Collections.Generic;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>
    /// Şehir kuşatma durumu (P14, ADR-012): şehrin kara komşu düğümlerinden (kara kenarı veya boğaz
    /// geçişi) düşman birlikli ya da düşman kontrolündeki bölgede olanlar sayılır. Hepsi kapalıysa kara
    /// yolu kesiktir; liman şehrinde deniz yolu da ablukayla kesik olmalıdır. Kara komşusu olmayan ada
    /// limanı yalnız ablukayla kuşatılır. Kısmen kapalı → <see cref="SiegeState.Encircling"/>.
    /// </summary>
    public static class GeoSiegeRules
    {
        public static void Update(GeoWarfareContext ctx)
        {
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                city.Siege = Evaluate(ctx, city);
            }
        }

        public static SiegeState Evaluate(GeoWarfareContext ctx, City city)
        {
            int land = 0;
            int closed = 0;
            IReadOnlyList<TransportEdge> edges = ctx.World.Transport.EdgesOf(city.Node);
            for (int e = 0; e < edges.Count; e++)
            {
                if (edges[e].IsNaval)
                {
                    continue;
                }

                land++;
                var next = edges[e].Other(city.Node);
                ctx.World.Transport.TryGetNode(next, out TransportNode node);
                bool hostileRegion = ctx.World.TryGetControl(node.Region, out RegionControl control) && ctx.IsHostile(city.Controller, control.Controller);
                if (hostileRegion || GeoUnitQueries.HasHostileUnitsAt(ctx, next, city.Controller))
                {
                    closed++;
                }
            }

            bool port = ctx.World.IsPort(city);
            bool blockaded = port && ctx.Warfare.IsBlockaded(city.Id);
            if (land == 0)
            {
                return blockaded ? SiegeState.Besieged : SiegeState.None;
            }

            if (closed == land && (!port || blockaded))
            {
                return SiegeState.Besieged;
            }

            return closed > 0 || blockaded ? SiegeState.Encircling : SiegeState.None;
        }
    }
}
