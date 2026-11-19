using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Birlik türü (tanım) kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct UnitTypeId : IStableId, IEquatable<UnitTypeId>, IComparable<UnitTypeId>
    {
        public static readonly UnitTypeId None = new UnitTypeId(0UL);

        private readonly ulong _value;

        public UnitTypeId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(UnitTypeId a, UnitTypeId b) => a._value == b._value;

        public static bool operator !=(UnitTypeId a, UnitTypeId b) => a._value != b._value;

        public static bool operator <(UnitTypeId a, UnitTypeId b) => a._value < b._value;

        public static bool operator >(UnitTypeId a, UnitTypeId b) => a._value > b._value;

        public static bool operator <=(UnitTypeId a, UnitTypeId b) => a._value <= b._value;

        public static bool operator >=(UnitTypeId a, UnitTypeId b) => a._value >= b._value;

        public bool Equals(UnitTypeId other) => _value == other._value;

        public override bool Equals(object obj) => obj is UnitTypeId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(UnitTypeId other) => _value.CompareTo(other._value);

        public override string ToString() => "UnitTypeId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
