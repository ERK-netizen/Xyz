using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Map;
using TarihStrateji.Core.World.Pathfinding;

namespace TarihStrateji.Core.Warfare.Rules
{
    /// <summary>
    /// Deniz rotası maliyeti: deniz hücreleri ve limanlı şehir hücreleri girilebilir, maliyet km'dir.
    /// Limanın kime ait olduğu rotada değil hareket anında (<see cref="NavalQueries.CanFleetEnter"/>)
    /// denetlenir; böylece model ülkeden bağımsız kalır ve bağlantı bileşenleri paylaşılabilir.
    /// </summary>
    public sealed class NavalMovementCostModel : IMovementCostModel
    {
        private readonly WorldMap _map;

        public NavalMovementCostModel(WorldMap map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Fixed64 MinimumCostPerKm => Fixed64.One;

        public bool CanEnter(HexCell cell)
        {
            return cell.IsWater || NavalQueries.HasPort(_map, cell);
        }

        public bool TryGetStepCost(HexCell from, HexCell to, Fixed64 km, out Fixed64 cost)
        {
            if (!CanEnter(from) || !CanEnter(to))
            {
                cost = Fixed64.Zero;
                return false;
            }

            cost = km;
            return true;
        }
    }
}
