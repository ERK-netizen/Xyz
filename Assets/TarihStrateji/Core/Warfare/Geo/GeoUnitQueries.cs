using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Warfare.Geo
{
    /// <summary>Coğrafi konum sorguları (birlik kimliği sırasıyla, determinist).</summary>
    public static class GeoUnitQueries
    {
        /// <summary>Düğümde duran (kenar üstünde olmayan, gemide olmayan) birlikler.</summary>
        public static void UnitsAtNode(GeoWarfareContext ctx, TransportNodeId node, List<LandUnit> into)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Node == node && u.RouteProgress == 0L && !u.IsEmbarked)
                {
                    into.Add(u);
                }
            }
        }

        public static bool HasHostileUnitsAt(GeoWarfareContext ctx, TransportNodeId node, CountryId country)
        {
            for (int i = 0; i < ctx.Warfare.Units.Count; i++)
            {
                LandUnit u = ctx.Warfare.Units.ValueAt(i);
                if (u.Node == node && u.RouteProgress == 0L && !u.IsEmbarked && ctx.IsHostile(country, u.Owner))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Düğümdeki şehir (yoksa null).</summary>
        public static City CityAt(GeoWarfareContext ctx, TransportNodeId node)
        {
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (city.Node == node)
                {
                    return city;
                }
            }

            return null;
        }

        /// <summary>Düşman kontrolündeki şehir veya düşman birlik varsa düğüme yürüyerek girilemez (saldırı gerekir).</summary>
        public static bool IsContested(GeoWarfareContext ctx, TransportNodeId node, CountryId country)
        {
            if (HasHostileUnitsAt(ctx, node, country))
            {
                return true;
            }

            City city = CityAt(ctx, node);
            return city != null && ctx.IsHostile(country, city.Controller);
        }

        /// <summary>
        /// Hareket hedefinin düğümü: şehir → şehir düğümü; birlik → düğümü; bölge → bölgedeki en küçük kimlikli
        /// şehir, yoksa en küçük kimlikli kara düğümü; nokta → noktanın bölgesindeki en yakın kara düğümü
        /// (eşitlikte küçük kimlik). Bulunamazsa <see cref="TransportNodeId.None"/>.
        /// </summary>
        public static TransportNodeId ResolveNode(GeoWarfareContext ctx, TargetRef target)
        {
            switch (target.Kind)
            {
                case TargetKind.City:
                    return ctx.World.TryGetCity(new CityId(target.EntityId), out City city) ? city.Node : TransportNodeId.None;
                case TargetKind.LandUnit:
                    return ctx.Warfare.Units.TryGetValue(new LandUnitId(target.EntityId), out LandUnit unit) && !unit.IsEmbarked
                        ? unit.Node
                        : TransportNodeId.None;
                case TargetKind.Region:
                    return RegionNode(ctx, new RegionId(target.EntityId));
                case TargetKind.Point:
                    return NearestNodeInRegion(ctx, ctx.World.Atlas.FindRegion(target.Point), target.Point);
                default:
                    return TransportNodeId.None;
            }
        }

        private static TransportNodeId RegionNode(GeoWarfareContext ctx, RegionId region)
        {
            for (int i = 0; i < ctx.World.Cities.Count; i++)
            {
                ctx.World.TryGetCity(ctx.World.Cities[i], out City city);
                if (city.Region == region)
                {
                    return city.Node;
                }
            }

            IReadOnlyList<TransportNodeId> nodes = ctx.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ctx.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                if (node.IsLand && node.Region == region)
                {
                    return node.Id;
                }
            }

            return TransportNodeId.None;
        }

        private static TransportNodeId NearestNodeInRegion(GeoWarfareContext ctx, RegionId region, GeoPoint point)
        {
            if (!region.IsValid)
            {
                return TransportNodeId.None;
            }

            TransportNodeId best = TransportNodeId.None;
            long bestDistance = long.MaxValue;
            IReadOnlyList<TransportNodeId> nodes = ctx.World.Transport.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                ctx.World.Transport.TryGetNode(nodes[i], out TransportNode node);
                if (!node.IsLand || node.Region != region)
                {
                    continue;
                }

                long d = GeoMath.DistanceMeters(point, node.Location);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = node.Id;
                }
            }

            return best;
        }
    }
}
