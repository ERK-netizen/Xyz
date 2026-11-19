using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Savaş kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct WarId : IStableId, IEquatable<WarId>, IComparable<WarId>
    {
        public static readonly WarId None = new WarId(0UL);

        private readonly ulong _value;

        public WarId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(WarId a, WarId b) => a._value == b._value;

        public static bool operator !=(WarId a, WarId b) => a._value != b._value;

        public static bool operator <(WarId a, WarId b) => a._value < b._value;

        public static bool operator >(WarId a, WarId b) => a._value > b._value;

        public static bool operator <=(WarId a, WarId b) => a._value <= b._value;

        public static bool operator >=(WarId a, WarId b) => a._value >= b._value;

        public bool Equals(WarId other) => _value == other._value;

        public override bool Equals(object obj) => obj is WarId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(WarId other) => _value.CompareTo(other._value);

        public override string ToString() => "WarId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
