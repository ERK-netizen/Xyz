using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Kriz türü kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct CrisisId : IStableId, IEquatable<CrisisId>, IComparable<CrisisId>
    {
        public static readonly CrisisId None = new CrisisId(0UL);

        private readonly ulong _value;

        public CrisisId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(CrisisId a, CrisisId b) => a._value == b._value;

        public static bool operator !=(CrisisId a, CrisisId b) => a._value != b._value;

        public static bool operator <(CrisisId a, CrisisId b) => a._value < b._value;

        public static bool operator >(CrisisId a, CrisisId b) => a._value > b._value;

        public static bool operator <=(CrisisId a, CrisisId b) => a._value <= b._value;

        public static bool operator >=(CrisisId a, CrisisId b) => a._value >= b._value;

        public bool Equals(CrisisId other) => _value == other._value;

        public override bool Equals(object obj) => obj is CrisisId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(CrisisId other) => _value.CompareTo(other._value);

        public override string ToString() => "CrisisId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
