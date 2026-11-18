using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Siyasi parti kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct PartyId : IStableId, IEquatable<PartyId>, IComparable<PartyId>
    {
        public static readonly PartyId None = new PartyId(0UL);

        private readonly ulong _value;

        public PartyId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(PartyId a, PartyId b) => a._value == b._value;

        public static bool operator !=(PartyId a, PartyId b) => a._value != b._value;

        public static bool operator <(PartyId a, PartyId b) => a._value < b._value;

        public static bool operator >(PartyId a, PartyId b) => a._value > b._value;

        public static bool operator <=(PartyId a, PartyId b) => a._value <= b._value;

        public static bool operator >=(PartyId a, PartyId b) => a._value >= b._value;

        public bool Equals(PartyId other) => _value == other._value;

        public override bool Equals(object obj) => obj is PartyId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(PartyId other) => _value.CompareTo(other._value);

        public override string ToString() => "PartyId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
