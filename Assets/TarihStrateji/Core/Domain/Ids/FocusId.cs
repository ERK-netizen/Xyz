using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Ulusal odak kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct FocusId : IStableId, IEquatable<FocusId>, IComparable<FocusId>
    {
        public static readonly FocusId None = new FocusId(0UL);

        private readonly ulong _value;

        public FocusId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(FocusId a, FocusId b) => a._value == b._value;

        public static bool operator !=(FocusId a, FocusId b) => a._value != b._value;

        public static bool operator <(FocusId a, FocusId b) => a._value < b._value;

        public static bool operator >(FocusId a, FocusId b) => a._value > b._value;

        public static bool operator <=(FocusId a, FocusId b) => a._value <= b._value;

        public static bool operator >=(FocusId a, FocusId b) => a._value >= b._value;

        public bool Equals(FocusId other) => _value == other._value;

        public override bool Equals(object obj) => obj is FocusId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(FocusId other) => _value.CompareTo(other._value);

        public override string ToString() => "FocusId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
