using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Ticaret emri kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct TradeOrderId : IStableId, IEquatable<TradeOrderId>, IComparable<TradeOrderId>
    {
        public static readonly TradeOrderId None = new TradeOrderId(0UL);

        private readonly ulong _value;

        public TradeOrderId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(TradeOrderId a, TradeOrderId b) => a._value == b._value;

        public static bool operator !=(TradeOrderId a, TradeOrderId b) => a._value != b._value;

        public static bool operator <(TradeOrderId a, TradeOrderId b) => a._value < b._value;

        public static bool operator >(TradeOrderId a, TradeOrderId b) => a._value > b._value;

        public static bool operator <=(TradeOrderId a, TradeOrderId b) => a._value <= b._value;

        public static bool operator >=(TradeOrderId a, TradeOrderId b) => a._value >= b._value;

        public bool Equals(TradeOrderId other) => _value == other._value;

        public override bool Equals(object obj) => obj is TradeOrderId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(TradeOrderId other) => _value.CompareTo(other._value);

        public override string ToString() => "TradeOrderId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
