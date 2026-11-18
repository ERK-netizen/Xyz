using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Teknoloji kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct TechnologyId : IStableId, IEquatable<TechnologyId>, IComparable<TechnologyId>
    {
        public static readonly TechnologyId None = new TechnologyId(0UL);

        private readonly ulong _value;

        public TechnologyId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(TechnologyId a, TechnologyId b) => a._value == b._value;

        public static bool operator !=(TechnologyId a, TechnologyId b) => a._value != b._value;

        public static bool operator <(TechnologyId a, TechnologyId b) => a._value < b._value;

        public static bool operator >(TechnologyId a, TechnologyId b) => a._value > b._value;

        public static bool operator <=(TechnologyId a, TechnologyId b) => a._value <= b._value;

        public static bool operator >=(TechnologyId a, TechnologyId b) => a._value >= b._value;

        public bool Equals(TechnologyId other) => _value == other._value;

        public override bool Equals(object obj) => obj is TechnologyId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(TechnologyId other) => _value.CompareTo(other._value);

        public override string ToString() => "TechnologyId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
