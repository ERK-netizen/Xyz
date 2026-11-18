using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Tesis kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct FacilityId : IStableId, IEquatable<FacilityId>, IComparable<FacilityId>
    {
        public static readonly FacilityId None = new FacilityId(0UL);

        private readonly ulong _value;

        public FacilityId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(FacilityId a, FacilityId b) => a._value == b._value;

        public static bool operator !=(FacilityId a, FacilityId b) => a._value != b._value;

        public static bool operator <(FacilityId a, FacilityId b) => a._value < b._value;

        public static bool operator >(FacilityId a, FacilityId b) => a._value > b._value;

        public static bool operator <=(FacilityId a, FacilityId b) => a._value <= b._value;

        public static bool operator >=(FacilityId a, FacilityId b) => a._value >= b._value;

        public bool Equals(FacilityId other) => _value == other._value;

        public override bool Equals(object obj) => obj is FacilityId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(FacilityId other) => _value.CompareTo(other._value);

        public override string ToString() => "FacilityId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
