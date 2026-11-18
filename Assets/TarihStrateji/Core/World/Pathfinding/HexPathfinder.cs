using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geo;
using TarihStrateji.Core.World.Grid;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.World.Pathfinding
{
    /// <summary>
    /// Dünya altıgen grafında A* (game-ai becerisi). Kurallar:
    /// <list type="bullet">
    /// <item>Hedef yığından çekilirken denetlenir; kenar gevşetmesi farklı maliyetlerde doğrudur.</item>
    /// <item>Sezgisel kabul edilebilirdir: max(adım × en kısa komşu km, |Δsatır| × en kısa çapraz
    /// km) × en küçük çarpan, eksi yuvarlama payı. Dijkstra ile eşitlik testlerle doğrulanır.</item>
    /// <item>Ulaşılamaz hedef, bağlantı bileşeni etiketleriyle aramaya girmeden reddedilir;
    /// ayrıca genişletme sınırı vardır.</item>
    /// <item>Komşular sabit yön sırasıyla, eşit f değerleri ekleme sırasıyla işlenir: deterministik.</item>
    /// </list>
    /// Tamponlar aramalar arasında yeniden kullanılır; örnek tek iş parçacığına aittir.
    /// Arazi veya geçilebilirlik değişince <see cref="InvalidateConnectivity"/> çağrılır.
    /// </summary>
    public sealed class HexPathfinder
    {
        private readonly WorldMap _map;
        private readonly IMovementCostModel _model;
        private readonly long[] _g;
        private readonly int[] _parent;
        private readonly int[] _seenStamp;
        private readonly int[] _closedStamp;
        private readonly PathNodeHeap _open = new PathNodeHeap();
        private int[] _component;
        private int _stamp;

        public HexPathfinder(WorldMap map, IMovementCostModel model)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            int n = map.CellCount;
            _g = new long[n];
            _parent = new int[n];
            _seenStamp = new int[n];
            _closedStamp = new int[n];
        }

        /// <summary>Bağlantı bileşeni kimliği; girilemeyen hücre için -1.</summary>
        public int ComponentOf(HexCoord coord)
        {
            EnsureConnectivity();
            return _component[_map.Grid.IndexOf(coord)];
        }

        public void InvalidateConnectivity()
        {
            _component = null;
        }

        /// <param name="maxExpansions">Genişletilecek en fazla düğüm; 0 = sınırsız (hücre sayısı).</param>
        public PathResult FindPath(HexCoord start, HexCoord goal, int maxExpansions = 0)
        {
            HexGrid grid = _map.Grid;
            if (!grid.Contains(start) || !grid.Contains(goal))
            {
                return PathResult.Failure(WorldErrorKeys.RotaBaslangicGecersiz, 0);
            }

            int startIndex = grid.IndexOf(start);
            int goalIndex = grid.IndexOf(goal);
            if (!_model.CanEnter(_map.CellAt(startIndex)))
            {
                return PathResult.Failure(WorldErrorKeys.RotaBaslangicGecersiz, 0);
            }

            EnsureConnectivity();
            if (_component[goalIndex] < 0 || _component[goalIndex] != _component[startIndex])
            {
                return PathResult.Failure(WorldErrorKeys.RotaUlasilamaz, 0);
            }

            if (startIndex == goalIndex)
            {
                return PathResult.Success(new[] { start }, Fixed64.Zero, Fixed64.Zero, 0);
            }

            int limit = maxExpansions <= 0 ? _map.CellCount : maxExpansions;
            NextStamp();
            _open.Clear();
            _g[startIndex] = 0L;
            _parent[startIndex] = -1;
            _seenStamp[startIndex] = _stamp;
            _open.Push(Heuristic(start, goal), startIndex);

            int expanded = 0;
            HexDistanceTable distances = _map.Distances;
            while (_open.Count > 0)
            {
                int current = _open.Pop();
                if (_closedStamp[current] == _stamp)
                {
                    continue; // tembel silme: daha iyi bir kopya önceden işlendi
                }

                if (current == goalIndex)
                {
                    return Reconstruct(startIndex, goalIndex, expanded);
                }

                _closedStamp[current] = _stamp;
                if (++expanded > limit)
                {
                    return PathResult.Failure(WorldErrorKeys.RotaAramaSiniri, expanded);
                }

                HexCell cell = _map.CellAt(current);
                for (int d = 0; d < HexDirections.Count; d++)
                {
                    var direction = (HexDirection)d;
                    if (!grid.TryGetNeighbor(cell.Coord, direction, out HexCoord nCoord))
                    {
                        continue;
                    }

                    int next = grid.IndexOf(nCoord);
                    if (_closedStamp[next] == _stamp)
                    {
                        continue;
                    }

                    distances.TryGetNeighborKm(cell.Coord, direction, out Fixed64 km);
                    if (!_model.TryGetStepCost(cell, _map.CellAt(next), km, out Fixed64 stepCost))
                    {
                        continue;
                    }

                    long tentative = checked(_g[current] + stepCost.Raw);
                    if (_seenStamp[next] != _stamp || tentative < _g[next])
                    {
                        _seenStamp[next] = _stamp;
                        _g[next] = tentative;
                        _parent[next] = current;
                        _open.Push(checked(tentative + Heuristic(nCoord, goal)), next);
                    }
                }
            }

            return PathResult.Failure(WorldErrorKeys.RotaUlasilamaz, expanded);
        }

        /// <summary>Kabul edilebilir alt sınır (ham Fixed64 birimi).</summary>
        private long Heuristic(HexCoord from, HexCoord goal)
        {
            HexDistanceTable distances = _map.Distances;
            int steps = _map.Grid.StepDistance(from, goal);
            if (steps == 0)
            {
                return 0L;
            }

            int rowDelta = System.Math.Abs(goal.Row - from.Row);
            Fixed64 byStep = distances.MinimumNeighborKm * steps;
            Fixed64 byRow = distances.MinimumRowChangeKm * rowDelta;
            long bound = (Fixed64.Max(byStep, byRow) * _model.MinimumCostPerKm).Raw - steps - 1L;
            return bound > 0L ? bound : 0L;
        }

        private PathResult Reconstruct(int startIndex, int goalIndex, int expanded)
        {
            int length = 1;
            for (int node = goalIndex; node != startIndex; node = _parent[node])
            {
                length++;
            }

            var path = new HexCoord[length];
            long kmRaw = 0L;
            int at = length - 1;
            for (int node = goalIndex; ; node = _parent[node])
            {
                path[at--] = _map.Grid.FromIndex(node);
                if (node == startIndex)
                {
                    break;
                }
            }

            for (int i = 1; i < path.Length; i++)
            {
                kmRaw = checked(kmRaw + NeighborKm(path[i - 1], path[i]).Raw);
            }

            return PathResult.Success(path, Fixed64.FromRaw(_g[goalIndex]), Fixed64.FromRaw(kmRaw), expanded);
        }

        private Fixed64 NeighborKm(HexCoord a, HexCoord b)
        {
            for (int d = 0; d < HexDirections.Count; d++)
            {
                if (_map.Grid.TryGetNeighbor(a, (HexDirection)d, out HexCoord n) && n == b)
                {
                    _map.Distances.TryGetNeighborKm(a, (HexDirection)d, out Fixed64 km);
                    return km;
                }
            }

            throw new InvalidOperationException("Rota komşu olmayan hücreler içeriyor.");
        }

        private void NextStamp()
        {
            if (_stamp == int.MaxValue)
            {
                Array.Clear(_seenStamp, 0, _seenStamp.Length);
                Array.Clear(_closedStamp, 0, _closedStamp.Length);
                _stamp = 0;
            }

            _stamp++;
        }

        private void EnsureConnectivity()
        {
            if (_component != null)
            {
                return;
            }

            HexGrid grid = _map.Grid;
            int n = _map.CellCount;
            var component = new int[n];
            var queue = new int[n];
            for (int i = 0; i < n; i++)
            {
                component[i] = -1;
            }

            int next = 0;
            for (int i = 0; i < n; i++)
            {
                if (component[i] >= 0 || !_model.CanEnter(_map.CellAt(i)))
                {
                    continue;
                }

                int head = 0;
                int tail = 0;
                queue[tail++] = i;
                component[i] = next;
                while (head < tail)
                {
                    int current = queue[head++];
                    HexCoord coord = grid.FromIndex(current);
                    for (int d = 0; d < HexDirections.Count; d++)
                    {
                        if (!grid.TryGetNeighbor(coord, (HexDirection)d, out HexCoord nCoord))
                        {
                            continue;
                        }

                        int neighbor = grid.IndexOf(nCoord);
                        if (component[neighbor] < 0 && _model.CanEnter(_map.CellAt(neighbor)))
                        {
                            component[neighbor] = next;
                            queue[tail++] = neighbor;
                        }
                    }
                }

                next++;
            }

            _component = component;
        }
    }
}
