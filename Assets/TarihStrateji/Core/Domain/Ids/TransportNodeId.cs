using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Ulaşım grafı düğüm kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct TransportNodeId : IStableId, IEquatable<TransportNodeId>, IComparable<TransportNodeId>
    {
        public static readonly TransportNodeId None = new TransportNodeId(0UL);

        private readonly ulong _value;

        public TransportNodeId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(TransportNodeId a, TransportNodeId b) => a._value == b._value;

        public static bool operator !=(TransportNodeId a, TransportNodeId b) => a._value != b._value;

        public static bool operator <(TransportNodeId a, TransportNodeId b) => a._value < b._value;

        public static bool operator >(TransportNodeId a, TransportNodeId b) => a._value > b._value;

        public static bool operator <=(TransportNodeId a, TransportNodeId b) => a._value <= b._value;

        public static bool operator >=(TransportNodeId a, TransportNodeId b) => a._value >= b._value;

        public bool Equals(TransportNodeId other) => _value == other._value;

        public override bool Equals(object obj) => obj is TransportNodeId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(TransportNodeId other) => _value.CompareTo(other._value);

        public override string ToString() => "TransportNodeId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
