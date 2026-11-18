using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>İnşaat siparişi kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct ConstructionId : IStableId, IEquatable<ConstructionId>, IComparable<ConstructionId>
    {
        public static readonly ConstructionId None = new ConstructionId(0UL);

        private readonly ulong _value;

        public ConstructionId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(ConstructionId a, ConstructionId b) => a._value == b._value;

        public static bool operator !=(ConstructionId a, ConstructionId b) => a._value != b._value;

        public static bool operator <(ConstructionId a, ConstructionId b) => a._value < b._value;

        public static bool operator >(ConstructionId a, ConstructionId b) => a._value > b._value;

        public static bool operator <=(ConstructionId a, ConstructionId b) => a._value <= b._value;

        public static bool operator >=(ConstructionId a, ConstructionId b) => a._value >= b._value;

        public bool Equals(ConstructionId other) => _value == other._value;

        public override bool Equals(object obj) => obj is ConstructionId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(ConstructionId other) => _value.CompareTo(other._value);

        public override string ToString() => "ConstructionId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
