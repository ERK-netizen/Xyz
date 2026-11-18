using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Hava kanadı kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct AirWingId : IStableId, IEquatable<AirWingId>, IComparable<AirWingId>
    {
        public static readonly AirWingId None = new AirWingId(0UL);

        private readonly ulong _value;

        public AirWingId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(AirWingId a, AirWingId b) => a._value == b._value;

        public static bool operator !=(AirWingId a, AirWingId b) => a._value != b._value;

        public static bool operator <(AirWingId a, AirWingId b) => a._value < b._value;

        public static bool operator >(AirWingId a, AirWingId b) => a._value > b._value;

        public static bool operator <=(AirWingId a, AirWingId b) => a._value <= b._value;

        public static bool operator >=(AirWingId a, AirWingId b) => a._value >= b._value;

        public bool Equals(AirWingId other) => _value == other._value;

        public override bool Equals(object obj) => obj is AirWingId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(AirWingId other) => _value.CompareTo(other._value);

        public override string ToString() => "AirWingId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
