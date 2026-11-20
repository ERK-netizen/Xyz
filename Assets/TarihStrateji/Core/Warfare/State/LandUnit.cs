using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Warfare.Definitions;

namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>
    /// Kara birliği (düz veri), ulaşım grafında bir düğümde ya da iki düğüm arasındaki yolda durur. Güç = min(asker oranı,
    /// ekipman düzeyi). Askerler <see cref="HomeRegion"/> nüfusundan seferber edilmiştir.
    /// </summary>
    public sealed class LandUnit
    {
        internal LandUnit(LandUnitId id, CountryId owner, UnitTypeId type, TransportNodeId node, RegionId homeRegion)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Node = node;
            HomeRegion = homeRegion;
        }

        public LandUnitId Id { get; }

        public CountryId Owner { get; }

        public UnitTypeId Type { get; }

        public RegionId HomeRegion { get; }

        public long Manpower { get; internal set; }

        /// <summary>Ekipman düzeyi [0, 1].</summary>
        public Fixed64 Equipment { get; internal set; }

        public Fixed64 Organization { get; internal set; }

        public Fixed64 ActionPoints { get; internal set; }

        /// <summary>Bugünkü ikmal düzeyi [0, 1] (P11).</summary>
        public Fixed64 Supply { get; internal set; }

        /// <summary>Son anında çözümün kimliği; defterdeyse P13 bu birliği atlar.</summary>
        public ResolutionId LastResolution { get; internal set; }

        /// <summary>Gemideyse taşıyan filo; değilse <see cref="FleetId.None"/>. Gemideki birliğin konumu filonun konumudur.</summary>
        public FleetId Carrier { get; internal set; }

        public bool IsEmbarked => Carrier.IsValid;

        /// <summary>
        /// Son ulaşılan ulaşım düğümü (ADR-011).
        /// </summary>
        public TransportNodeId Node { get; internal set; }

        internal List<TransportEdgeId> RouteList { get; } = new List<TransportEdgeId>();

        /// <summary>Kalan rota kenarları (sıradaki önce); ilk kenarda <see cref="RouteProgress"/> kadar yol alınmıştır.</summary>
        public IReadOnlyList<TransportEdgeId> Route => RouteList;

        /// <summary>Rotanın ilk kenarında harcanmış hareket maliyeti (kenar üstündeki ara konum).</summary>
        public long RouteProgress { get; internal set; }

        /// <summary>Coğrafi saldırı emri hedefi (birlik veya şehir); yoksa <see cref="TargetRef.None"/>.</summary>
        public TargetRef AttackTarget { get; internal set; }

        public Fixed64 Strength(UnitTypeDefinition type)
        {
            return Fixed64.Min(Fixed64.FromRatio(Manpower, type.Manpower), Equipment);
        }
    }
}
