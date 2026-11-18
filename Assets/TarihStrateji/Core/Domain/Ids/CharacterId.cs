using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Karakter (lider, general, bakan) kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct CharacterId : IStableId, IEquatable<CharacterId>, IComparable<CharacterId>
    {
        public static readonly CharacterId None = new CharacterId(0UL);

        private readonly ulong _value;

        public CharacterId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(CharacterId a, CharacterId b) => a._value == b._value;

        public static bool operator !=(CharacterId a, CharacterId b) => a._value != b._value;

        public static bool operator <(CharacterId a, CharacterId b) => a._value < b._value;

        public static bool operator >(CharacterId a, CharacterId b) => a._value > b._value;

        public static bool operator <=(CharacterId a, CharacterId b) => a._value <= b._value;

        public static bool operator >=(CharacterId a, CharacterId b) => a._value >= b._value;

        public bool Equals(CharacterId other) => _value == other._value;

        public override bool Equals(object obj) => obj is CharacterId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(CharacterId other) => _value.CompareTo(other._value);

        public override string ToString() => "CharacterId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
