using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Kara birliği kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct LandUnitId : IStableId, IEquatable<LandUnitId>, IComparable<LandUnitId>
    {
        public static readonly LandUnitId None = new LandUnitId(0UL);

        private readonly ulong _value;

        public LandUnitId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(LandUnitId a, LandUnitId b) => a._value == b._value;

        public static bool operator !=(LandUnitId a, LandUnitId b) => a._value != b._value;

        public static bool operator <(LandUnitId a, LandUnitId b) => a._value < b._value;

        public static bool operator >(LandUnitId a, LandUnitId b) => a._value > b._value;

        public static bool operator <=(LandUnitId a, LandUnitId b) => a._value <= b._value;

        public static bool operator >=(LandUnitId a, LandUnitId b) => a._value >= b._value;

        public bool Equals(LandUnitId other) => _value == other._value;

        public override bool Equals(object obj) => obj is LandUnitId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(LandUnitId other) => _value.CompareTo(other._value);

        public override string ToString() => "LandUnitId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
