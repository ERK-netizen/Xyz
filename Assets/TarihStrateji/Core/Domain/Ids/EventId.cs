using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Olay kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct EventId : IStableId, IEquatable<EventId>, IComparable<EventId>
    {
        public static readonly EventId None = new EventId(0UL);

        private readonly ulong _value;

        public EventId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(EventId a, EventId b) => a._value == b._value;

        public static bool operator !=(EventId a, EventId b) => a._value != b._value;

        public static bool operator <(EventId a, EventId b) => a._value < b._value;

        public static bool operator >(EventId a, EventId b) => a._value > b._value;

        public static bool operator <=(EventId a, EventId b) => a._value <= b._value;

        public static bool operator >=(EventId a, EventId b) => a._value >= b._value;

        public bool Equals(EventId other) => _value == other._value;

        public override bool Equals(object obj) => obj is EventId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(EventId other) => _value.CompareTo(other._value);

        public override string ToString() => "EventId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
