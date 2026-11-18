using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Filo kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct FleetId : IStableId, IEquatable<FleetId>, IComparable<FleetId>
    {
        public static readonly FleetId None = new FleetId(0UL);

        private readonly ulong _value;

        public FleetId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(FleetId a, FleetId b) => a._value == b._value;

        public static bool operator !=(FleetId a, FleetId b) => a._value != b._value;

        public static bool operator <(FleetId a, FleetId b) => a._value < b._value;

        public static bool operator >(FleetId a, FleetId b) => a._value > b._value;

        public static bool operator <=(FleetId a, FleetId b) => a._value <= b._value;

        public static bool operator >=(FleetId a, FleetId b) => a._value >= b._value;

        public bool Equals(FleetId other) => _value == other._value;

        public override bool Equals(object obj) => obj is FleetId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(FleetId other) => _value.CompareTo(other._value);

        public override string ToString() => "FleetId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
