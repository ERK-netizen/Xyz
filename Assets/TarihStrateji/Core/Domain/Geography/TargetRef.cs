using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Geography
{
    /// <summary>
    /// Emir hedefi: birlik, şehir, bölge, filo, hava üssü (kimlikle) veya coğrafi nokta. Hücre veya
    /// sektör kimliği taşımaz (EK A.1). Sunum katmanı yalnız bunu üretir; doğrulama çekirdektedir.
    /// </summary>
    [Serializable]
    public readonly struct TargetRef : IEquatable<TargetRef>
    {
        public static readonly TargetRef None = default;

        private TargetRef(TargetKind kind, ulong entityId, GeoPoint point)
        {
            Kind = kind;
            EntityId = entityId;
            Point = point;
        }

        public TargetKind Kind { get; }

        /// <summary>Varlık kimliğinin ham değeri; <see cref="TargetKind.Point"/> için 0.</summary>
        public ulong EntityId { get; }

        /// <summary>Yalnız <see cref="TargetKind.Point"/> için anlamlıdır.</summary>
        public GeoPoint Point { get; }

        public bool IsValid => Kind == TargetKind.Point || (Kind != TargetKind.None && EntityId != 0UL);

        public static TargetRef Entity(TargetKind kind, ulong entityId)
        {
            if (kind == TargetKind.None || kind == TargetKind.Point || entityId == 0UL)
            {
                throw new ArgumentException("Varlık hedefi için tür ve sıfır olmayan kimlik gerekir.", nameof(kind));
            }

            return new TargetRef(kind, entityId, default);
        }

        public static TargetRef AtPoint(GeoPoint point) => new TargetRef(TargetKind.Point, 0UL, point);

        public bool Equals(TargetRef other) => Kind == other.Kind && EntityId == other.EntityId && Point == other.Point;

        public override bool Equals(object obj) => obj is TargetRef other && Equals(other);

        public override int GetHashCode() => ((int)Kind * 31) ^ EntityId.GetHashCode() ^ Point.GetHashCode();

        public override string ToString() =>
            Kind == TargetKind.Point ? "Hedef(Nokta " + Point + ")" : "Hedef(" + Kind + " " + EntityId.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
