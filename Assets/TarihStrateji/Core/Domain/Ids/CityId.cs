using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Şehir kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct CityId : IStableId, IEquatable<CityId>, IComparable<CityId>
    {
        public static readonly CityId None = new CityId(0UL);

        private readonly ulong _value;

        public CityId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(CityId a, CityId b) => a._value == b._value;

        public static bool operator !=(CityId a, CityId b) => a._value != b._value;

        public static bool operator <(CityId a, CityId b) => a._value < b._value;

        public static bool operator >(CityId a, CityId b) => a._value > b._value;

        public static bool operator <=(CityId a, CityId b) => a._value <= b._value;

        public static bool operator >=(CityId a, CityId b) => a._value >= b._value;

        public bool Equals(CityId other) => _value == other._value;

        public override bool Equals(object obj) => obj is CityId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(CityId other) => _value.CompareTo(other._value);

        public override string ToString() => "CityId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
