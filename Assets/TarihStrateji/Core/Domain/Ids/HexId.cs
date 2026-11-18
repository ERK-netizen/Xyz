using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Dünya altıgen hücresi kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct HexId : IStableId, IEquatable<HexId>, IComparable<HexId>
    {
        public static readonly HexId None = new HexId(0UL);

        private readonly ulong _value;

        public HexId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(HexId a, HexId b) => a._value == b._value;

        public static bool operator !=(HexId a, HexId b) => a._value != b._value;

        public static bool operator <(HexId a, HexId b) => a._value < b._value;

        public static bool operator >(HexId a, HexId b) => a._value > b._value;

        public static bool operator <=(HexId a, HexId b) => a._value <= b._value;

        public static bool operator >=(HexId a, HexId b) => a._value >= b._value;

        public bool Equals(HexId other) => _value == other._value;

        public override bool Equals(object obj) => obj is HexId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(HexId other) => _value.CompareTo(other._value);

        public override string ToString() => "HexId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
