using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Etki (modifiye edici) kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct EffectId : IStableId, IEquatable<EffectId>, IComparable<EffectId>
    {
        public static readonly EffectId None = new EffectId(0UL);

        private readonly ulong _value;

        public EffectId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(EffectId a, EffectId b) => a._value == b._value;

        public static bool operator !=(EffectId a, EffectId b) => a._value != b._value;

        public static bool operator <(EffectId a, EffectId b) => a._value < b._value;

        public static bool operator >(EffectId a, EffectId b) => a._value > b._value;

        public static bool operator <=(EffectId a, EffectId b) => a._value <= b._value;

        public static bool operator >=(EffectId a, EffectId b) => a._value >= b._value;

        public bool Equals(EffectId other) => _value == other._value;

        public override bool Equals(object obj) => obj is EffectId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(EffectId other) => _value.CompareTo(other._value);

        public override string ToString() => "EffectId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
