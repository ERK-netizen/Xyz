using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Antlaşma kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct TreatyId : IStableId, IEquatable<TreatyId>, IComparable<TreatyId>
    {
        public static readonly TreatyId None = new TreatyId(0UL);

        private readonly ulong _value;

        public TreatyId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(TreatyId a, TreatyId b) => a._value == b._value;

        public static bool operator !=(TreatyId a, TreatyId b) => a._value != b._value;

        public static bool operator <(TreatyId a, TreatyId b) => a._value < b._value;

        public static bool operator >(TreatyId a, TreatyId b) => a._value > b._value;

        public static bool operator <=(TreatyId a, TreatyId b) => a._value <= b._value;

        public static bool operator >=(TreatyId a, TreatyId b) => a._value >= b._value;

        public bool Equals(TreatyId other) => _value == other._value;

        public override bool Equals(object obj) => obj is TreatyId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(TreatyId other) => _value.CompareTo(other._value);

        public override string ToString() => "TreatyId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
