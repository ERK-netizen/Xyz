using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Ulaşım grafı düğümü: konum, bulunduğu bölge (deniz düğümünde <see cref="RegionId.None"/>), tür.</summary>
    public sealed class TransportNode
    {
        public TransportNode(TransportNodeId id, GeoPoint location, RegionId region, TransportNodeKind kind)
        {
            Id = id;
            Location = location;
            Region = region;
            Kind = kind;
        }

        public TransportNodeId Id { get; }

        public GeoPoint Location { get; }

        public RegionId Region { get; }

        public TransportNodeKind Kind { get; }

        public bool IsNaval => Kind == TransportNodeKind.Port || Kind == TransportNodeKind.Sea;

        public bool IsLand => Kind != TransportNodeKind.Sea;
    }
}
