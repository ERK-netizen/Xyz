using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Bulunan rota: düğümler (başlangıç dahil), kenarlar, toplam maliyet ve uzunluk.</summary>
    public sealed class TransportPath
    {
        public TransportPath(IReadOnlyList<TransportNodeId> nodes, IReadOnlyList<TransportEdgeId> edges, long cost, long lengthMeters)
        {
            Nodes = nodes;
            Edges = edges;
            Cost = cost;
            LengthMeters = lengthMeters;
        }

        public IReadOnlyList<TransportNodeId> Nodes { get; }

        public IReadOnlyList<TransportEdgeId> Edges { get; }

        public long Cost { get; }

        public long LengthMeters { get; }
    }
}
