using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Pathfinding
{
    /// <summary>Kara hareketi: maliyet = km × arazi(hedef) × altyapı(kenar). Su geçilemez.</summary>
    public sealed class LandMovementCostModel : IMovementCostModel
    {
        private readonly TerrainMovementTable _table;

        public LandMovementCostModel(TerrainMovementTable table)
        {
            _table = table ?? throw new ArgumentNullException(nameof(table));
        }

        public Fixed64 MinimumCostPerKm => _table.MinimumMultiplier;

        public bool CanEnter(HexCell cell)
        {
            return _table.IsPassable(cell.Terrain);
        }

        public bool TryGetStepCost(HexCell from, HexCell to, Fixed64 km, out Fixed64 cost)
        {
            if (!CanEnter(from) || !CanEnter(to))
            {
                cost = Fixed64.Zero;
                return false;
            }

            Fixed64 multiplier = _table.TerrainMultiplier(to.Terrain) * _table.InfrastructureMultiplier(from.Infrastructure, to.Infrastructure);
            cost = km * Fixed64.Max(multiplier, _table.MinimumMultiplier);
            return true;
        }
    }
}
