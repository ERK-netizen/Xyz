using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Şehir alt sektörü kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct SectorId : IStableId, IEquatable<SectorId>, IComparable<SectorId>
    {
        public static readonly SectorId None = new SectorId(0UL);

        private readonly ulong _value;

        public SectorId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(SectorId a, SectorId b) => a._value == b._value;

        public static bool operator !=(SectorId a, SectorId b) => a._value != b._value;

        public static bool operator <(SectorId a, SectorId b) => a._value < b._value;

        public static bool operator >(SectorId a, SectorId b) => a._value > b._value;

        public static bool operator <=(SectorId a, SectorId b) => a._value <= b._value;

        public static bool operator >=(SectorId a, SectorId b) => a._value >= b._value;

        public bool Equals(SectorId other) => _value == other._value;

        public override bool Equals(object obj) => obj is SectorId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(SectorId other) => _value.CompareTo(other._value);

        public override string ToString() => "SectorId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
