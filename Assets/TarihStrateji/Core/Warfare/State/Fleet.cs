using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>
    /// Filo: tek türden gemi grubu. Hücresi bir deniz hücresi veya sahip olunan bir liman şehri
    /// (limanda demirli) olur. Güç = min(mürettebat oranı, gemi oranı); kural mantığı
    /// <c>FleetRules</c> ve <c>NavalCombatRules</c>'tadır.
    /// </summary>
    public sealed class Fleet
    {
        internal Fleet(FleetId id, CountryId owner, UnitTypeId type, HexId hex, RegionId homeRegion)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Hex = hex;
            HomeRegion = homeRegion;
        }

        internal Fleet(FleetId id, CountryId owner, UnitTypeId type, TransportNodeId node, RegionId homeRegion)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Hex = HexId.None;
            Node = node;
            HomeRegion = homeRegion;
        }

        public FleetId Id { get; }

        public CountryId Owner { get; }

        public UnitTypeId Type { get; }

        public HexId Hex { get; internal set; }

        /// <summary>Mürettebatın seferber edildiği bölge (kayıplar ve terhis buraya yazılır).</summary>
        public RegionId HomeRegion { get; }

        public long Manpower { get; internal set; }

        public Fixed64 Equipment { get; internal set; }

        public Fixed64 Organization { get; internal set; }

        internal List<HexId> PathList { get; } = new List<HexId>();

        public IReadOnlyList<HexId> Path => PathList;

        public Fixed64 MoveBudget { get; internal set; }

        /// <summary>Coğrafi model: bulunduğu deniz/liman düğümü (ADR-011).</summary>
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
