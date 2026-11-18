using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Stratejik bölge kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct RegionId : IStableId, IEquatable<RegionId>, IComparable<RegionId>
    {
        public static readonly RegionId None = new RegionId(0UL);

        private readonly ulong _value;

        public RegionId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(RegionId a, RegionId b) => a._value == b._value;

        public static bool operator !=(RegionId a, RegionId b) => a._value != b._value;

        public static bool operator <(RegionId a, RegionId b) => a._value < b._value;

        public static bool operator >(RegionId a, RegionId b) => a._value > b._value;

        public static bool operator <=(RegionId a, RegionId b) => a._value <= b._value;

        public static bool operator >=(RegionId a, RegionId b) => a._value >= b._value;

        public bool Equals(RegionId other) => _value == other._value;

        public override bool Equals(object obj) => obj is RegionId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(RegionId other) => _value.CompareTo(other._value);

        public override string ToString() => "RegionId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
