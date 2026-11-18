using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Yönetim biçimi kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct GovernmentFormId : IStableId, IEquatable<GovernmentFormId>, IComparable<GovernmentFormId>
    {
        public static readonly GovernmentFormId None = new GovernmentFormId(0UL);

        private readonly ulong _value;

        public GovernmentFormId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(GovernmentFormId a, GovernmentFormId b) => a._value == b._value;

        public static bool operator !=(GovernmentFormId a, GovernmentFormId b) => a._value != b._value;

        public static bool operator <(GovernmentFormId a, GovernmentFormId b) => a._value < b._value;

        public static bool operator >(GovernmentFormId a, GovernmentFormId b) => a._value > b._value;

        public static bool operator <=(GovernmentFormId a, GovernmentFormId b) => a._value <= b._value;

        public static bool operator >=(GovernmentFormId a, GovernmentFormId b) => a._value >= b._value;

        public bool Equals(GovernmentFormId other) => _value == other._value;

        public override bool Equals(object obj) => obj is GovernmentFormId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(GovernmentFormId other) => _value.CompareTo(other._value);

        public override string ToString() => "GovernmentFormId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
