using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Tesis türü (tanım) kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct FacilityTypeId : IStableId, IEquatable<FacilityTypeId>, IComparable<FacilityTypeId>
    {
        public static readonly FacilityTypeId None = new FacilityTypeId(0UL);

        private readonly ulong _value;

        public FacilityTypeId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(FacilityTypeId a, FacilityTypeId b) => a._value == b._value;

        public static bool operator !=(FacilityTypeId a, FacilityTypeId b) => a._value != b._value;

        public static bool operator <(FacilityTypeId a, FacilityTypeId b) => a._value < b._value;

        public static bool operator >(FacilityTypeId a, FacilityTypeId b) => a._value > b._value;

        public static bool operator <=(FacilityTypeId a, FacilityTypeId b) => a._value <= b._value;

        public static bool operator >=(FacilityTypeId a, FacilityTypeId b) => a._value >= b._value;

        public bool Equals(FacilityTypeId other) => _value == other._value;

        public override bool Equals(object obj) => obj is FacilityTypeId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(FacilityTypeId other) => _value.CompareTo(other._value);

        public override string ToString() => "FacilityTypeId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
