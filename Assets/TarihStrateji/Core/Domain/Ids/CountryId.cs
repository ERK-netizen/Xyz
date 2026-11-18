using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Ülke kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct CountryId : IStableId, IEquatable<CountryId>, IComparable<CountryId>
    {
        public static readonly CountryId None = new CountryId(0UL);

        private readonly ulong _value;

        public CountryId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(CountryId a, CountryId b) => a._value == b._value;

        public static bool operator !=(CountryId a, CountryId b) => a._value != b._value;

        public static bool operator <(CountryId a, CountryId b) => a._value < b._value;

        public static bool operator >(CountryId a, CountryId b) => a._value > b._value;

        public static bool operator <=(CountryId a, CountryId b) => a._value <= b._value;

        public static bool operator >=(CountryId a, CountryId b) => a._value >= b._value;

        public bool Equals(CountryId other) => _value == other._value;

        public override bool Equals(object obj) => obj is CountryId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(CountryId other) => _value.CompareTo(other._value);

        public override string ToString() => "CountryId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
