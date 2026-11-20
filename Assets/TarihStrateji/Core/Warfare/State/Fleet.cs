using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>
    /// Filo: tek türden gemi grubu. Bir deniz düğümünde, deniz yolu üzerinde veya kendi kontrolündeki
    /// limanda (demirli) bulunur. Güç = min(mürettebat oranı, gemi oranı); kural mantığı
    /// <c>GeoNavalRules</c>'tadır.
    /// </summary>
    public sealed class Fleet
    {
        internal Fleet(FleetId id, CountryId owner, UnitTypeId type, TransportNodeId node, RegionId homeRegion)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Node = node;
            HomeRegion = homeRegion;
        }

        public FleetId Id { get; }

        public CountryId Owner { get; }

        public UnitTypeId Type { get; }

        /// <summary>Mürettebatın seferber edildiği bölge (kayıplar ve terhis buraya yazılır).</summary>
        public RegionId HomeRegion { get; }

        public long Manpower { get; internal set; }

        public Fixed64 Equipment { get; internal set; }

        public Fixed64 Organization { get; internal set; }

        /// <summary>Bulunduğu ya da son ulaştığı deniz/liman düğümü (ADR-011).</summary>
        public TransportNodeId Node { get; internal set; }

        internal List<TransportEdgeId> RouteList { get; } = new List<TransportEdgeId>();

        /// <summary>Kalan deniz rotası; ilk kenarda <see cref="RouteProgress"/> kadar yol alınmıştır.</summary>
        public IReadOnlyList<TransportEdgeId> Route => RouteList;

        public long RouteProgress { get; internal set; }

        public Fixed64 Strength(ShipTypeDefinition type)
        {
            return Fixed64.Min(Fixed64.FromRatio(Manpower, type.Crew), Equipment);
        }
    }
}
