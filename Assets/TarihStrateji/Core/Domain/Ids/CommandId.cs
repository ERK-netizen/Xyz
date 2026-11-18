using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Günlüğe işlenmiş emir kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct CommandId : IStableId, IEquatable<CommandId>, IComparable<CommandId>
    {
        public static readonly CommandId None = new CommandId(0UL);

        private readonly ulong _value;

        public CommandId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(CommandId a, CommandId b) => a._value == b._value;

        public static bool operator !=(CommandId a, CommandId b) => a._value != b._value;

        public static bool operator <(CommandId a, CommandId b) => a._value < b._value;

        public static bool operator >(CommandId a, CommandId b) => a._value > b._value;

        public static bool operator <=(CommandId a, CommandId b) => a._value <= b._value;

        public static bool operator >=(CommandId a, CommandId b) => a._value >= b._value;

        public bool Equals(CommandId other) => _value == other._value;

        public override bool Equals(object obj) => obj is CommandId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(CommandId other) => _value.CompareTo(other._value);

        public override string ToString() => "CommandId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
