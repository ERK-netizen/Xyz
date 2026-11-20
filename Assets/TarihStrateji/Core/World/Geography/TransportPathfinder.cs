using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Ulaşım grafında A* (§3.4): sezgisel = büyük çember km × <see cref="ITransportCostModel.MinCostPerKilometer"/>
    /// (kabul edilebilir, çünkü kenar uzunluğu hiçbir zaman büyük çemberden kısa değildir). Komşular kenar
    /// kimliği sırasıyla, eşit öncelikler ekleme sırasıyla açılır; sonuç platformdan bağımsızdır. Genişletme
    /// sınırı aşılırsa <see cref="GeographyErrorKeys.RotaAramaSiniri"/>.
    /// </summary>
    public sealed class TransportPathfinder
    {
        public const int DefaultExpansionLimit = 200_000;

        private readonly TransportGraph _graph;
        private readonly PathNodeHeap _open = new PathNodeHeap();
        private readonly Dictionary<TransportNodeId, int> _index = new Dictionary<TransportNodeId, int>();
        private readonly List<TransportNodeId> _ids = new List<TransportNodeId>();
        private readonly List<long> _g = new List<long>();
        private readonly List<int> _parent = new List<int>();
        private readonly List<TransportEdge> _parentEdge = new List<TransportEdge>();
        private readonly List<bool> _closed = new List<bool>();

        public TransportPathfinder(TransportGraph graph)
        {
            _graph = graph;
        }

        public Result<TransportPath> FindPath(TransportNodeId start, TransportNodeId goal, ITransportCostModel cost, int expansionLimit = DefaultExpansionLimit)
        {
            if (!_graph.TryGetNode(start, out TransportNode startNode) || !_graph.TryGetNode(goal, out TransportNode goalNode))
            {
                return Result<TransportPath>.Failure(GeographyErrorKeys.DugumBulunamadi);
            }

            Reset();
            int startIndex = Visit(start);
            _g[startIndex] = 0L;
            _open.Push(Heuristic(startNode, goalNode, cost), startIndex);
            int expansions = 0;

            while (_open.Count > 0)
            {
                int current = _open.Pop();
                if (_closed[current])
                {
                    continue;
                }

                if (_ids[current] == goal)
                {
                    return Result<TransportPath>.Success(Build(current));
                }

                _closed[current] = true;
                if (++expansions > expansionLimit)
                {
                    return Result<TransportPath>.Failure(GeographyErrorKeys.RotaAramaSiniri);
                }

                _graph.TryGetNode(_ids[current], out TransportNode from);
                IReadOnlyList<TransportEdge> edges = _graph.EdgesOf(from.Id);
                for (int i = 0; i < edges.Count; i++)
                {
                    TransportEdge edge = edges[i];
                    TransportNodeId nextId = edge.Other(from.Id);
                    _graph.TryGetNode(nextId, out TransportNode to);
                    if (!cost.TryGetCost(edge, from, to, out long step))
                    {
                        continue;
                    }

                    int next = Visit(nextId);
                    if (_closed[next])
                    {
                        continue;
                    }

                    long tentative = checked(_g[current] + step);
                    if (tentative < _g[next])
                    {
                        _g[next] = tentative;
                        _parent[next] = current;
                        _parentEdge[next] = edge;
                        _open.Push(checked(tentative + Heuristic(to, goalNode, cost)), next);
                    }
                }
            }

            return Result<TransportPath>.Failure(GeographyErrorKeys.RotaYok);
        }

        private static long Heuristic(TransportNode from, TransportNode goal, ITransportCostModel cost) =>
            GeoMath.DistanceMeters(from.Location, goal.Location) * cost.MinCostPerKilometer / 1000L;

        private TransportPath Build(int goalIndex)
        {
            var nodes = new List<TransportNodeId>();
            var edges = new List<TransportEdgeId>();
            long length = 0L;
            for (int i = goalIndex; i >= 0; i = _parent[i])
            {
                nodes.Add(_ids[i]);
                if (_parentEdge[i] != null)
                {
                    edges.Add(_parentEdge[i].Id);
                    length += _parentEdge[i].LengthMeters;
                }
            }

            nodes.Reverse();
            edges.Reverse();
            return new TransportPath(nodes, edges, _g[goalIndex], length);
        }

        private void Reset()
        {
            _open.Clear();
            _index.Clear();
            _ids.Clear();
            _g.Clear();
            _parent.Clear();
            _parentEdge.Clear();
            _closed.Clear();
        }

        private int Visit(TransportNodeId id)
        {
            if (_index.TryGetValue(id, out int index))
            {
                return index;
            }

            index = _ids.Count;
            _index.Add(id, index);
            _ids.Add(id);
            _g.Add(long.MaxValue);
            _parent.Add(-1);
            _parentEdge.Add(null);
            _closed.Add(false);
            return index;
        }
    }
}
