using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Gün içi anında çözüm kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct ResolutionId : IStableId, IEquatable<ResolutionId>, IComparable<ResolutionId>
    {
        public static readonly ResolutionId None = new ResolutionId(0UL);

        private readonly ulong _value;

        public ResolutionId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(ResolutionId a, ResolutionId b) => a._value == b._value;

        public static bool operator !=(ResolutionId a, ResolutionId b) => a._value != b._value;

        public static bool operator <(ResolutionId a, ResolutionId b) => a._value < b._value;

        public static bool operator >(ResolutionId a, ResolutionId b) => a._value > b._value;

        public static bool operator <=(ResolutionId a, ResolutionId b) => a._value <= b._value;

        public static bool operator >=(ResolutionId a, ResolutionId b) => a._value >= b._value;

        public bool Equals(ResolutionId other) => _value == other._value;

        public override bool Equals(object obj) => obj is ResolutionId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(ResolutionId other) => _value.CompareTo(other._value);

        public override string ToString() => "ResolutionId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
