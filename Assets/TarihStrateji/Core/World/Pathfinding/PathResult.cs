using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Grid;

namespace TarihStrateji.Core.World.Pathfinding
{
    /// <summary>Rota arama sonucu. Bulunamadıysa <see cref="ErrorKey"/> nedenini taşır.</summary>
    public sealed class PathResult
    {
        private static readonly HexCoord[] Empty = new HexCoord[0];

        private PathResult(bool found, string errorKey, HexCoord[] path, Fixed64 totalCost, Fixed64 totalKm, int expandedNodes)
        {
            Found = found;
            ErrorKey = errorKey;
            Path = path;
            TotalCost = totalCost;
            TotalKm = totalKm;
            ExpandedNodes = expandedNodes;
        }

        public bool Found { get; }

        public string ErrorKey { get; }

        /// <summary>Başlangıç ve hedef dahil hücreler.</summary>
        public IReadOnlyList<HexCoord> Path { get; }

        public Fixed64 TotalCost { get; }

        public Fixed64 TotalKm { get; }

        public int ExpandedNodes { get; }

        public int Steps => Path.Count == 0 ? 0 : Path.Count - 1;

        internal static PathResult Success(HexCoord[] path, Fixed64 totalCost, Fixed64 totalKm, int expanded)
        {
            return new PathResult(true, null, path, totalCost, totalKm, expanded);
        }

        internal static PathResult Failure(string errorKey, int expanded)
        {
            return new PathResult(false, errorKey, Empty, Fixed64.Zero, Fixed64.Zero, expanded);
        }
    }
}
