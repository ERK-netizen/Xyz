using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>Son görülen yabancı varlık: konum, (tahmini) güç ve görüldüğü gün.</summary>
    public sealed class ContactReport
    {
        internal ContactReport(SightingKind kind, ulong entityId, CountryId owner)
        {
            Kind = kind;
            EntityId = entityId;
            Owner = owner;
        }

        public SightingKind Kind { get; }

        public ulong EntityId { get; }

        public CountryId Owner { get; }

        public HexId LastHex { get; internal set; }

        public Fixed64 EstimatedStrength { get; internal set; }

        /// <summary>Gücün kesin mi (yakından) yoksa yuvarlanmış tahmin mi olduğu.</summary>
        public bool IsExact { get; internal set; }

        public GameDate SeenOn { get; internal set; }

        /// <summary>Sıralama anahtarı: (tür, kimlik).</summary>
        public ulong SortKey => ((ulong)Kind << 56) | EntityId;
    }
}
