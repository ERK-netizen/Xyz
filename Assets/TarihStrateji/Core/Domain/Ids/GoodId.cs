using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Mal/kaynak türü kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct GoodId : IStableId, IEquatable<GoodId>, IComparable<GoodId>
    {
        public static readonly GoodId None = new GoodId(0UL);

        private readonly ulong _value;

        public GoodId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(GoodId a, GoodId b) => a._value == b._value;

        public static bool operator !=(GoodId a, GoodId b) => a._value != b._value;

        public static bool operator <(GoodId a, GoodId b) => a._value < b._value;

        public static bool operator >(GoodId a, GoodId b) => a._value > b._value;

        public static bool operator <=(GoodId a, GoodId b) => a._value <= b._value;

        public static bool operator >=(GoodId a, GoodId b) => a._value >= b._value;

        public bool Equals(GoodId other) => _value == other._value;

        public override bool Equals(object obj) => obj is GoodId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(GoodId other) => _value.CompareTo(other._value);

        public override string ToString() => "GoodId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
