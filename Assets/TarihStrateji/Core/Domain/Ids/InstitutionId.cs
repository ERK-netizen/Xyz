using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Kurum türü kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct InstitutionId : IStableId, IEquatable<InstitutionId>, IComparable<InstitutionId>
    {
        public static readonly InstitutionId None = new InstitutionId(0UL);

        private readonly ulong _value;

        public InstitutionId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(InstitutionId a, InstitutionId b) => a._value == b._value;

        public static bool operator !=(InstitutionId a, InstitutionId b) => a._value != b._value;

        public static bool operator <(InstitutionId a, InstitutionId b) => a._value < b._value;

        public static bool operator >(InstitutionId a, InstitutionId b) => a._value > b._value;

        public static bool operator <=(InstitutionId a, InstitutionId b) => a._value <= b._value;

        public static bool operator >=(InstitutionId a, InstitutionId b) => a._value >= b._value;

        public bool Equals(InstitutionId other) => _value == other._value;

        public override bool Equals(object obj) => obj is InstitutionId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(InstitutionId other) => _value.CompareTo(other._value);

        public override string ToString() => "InstitutionId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
