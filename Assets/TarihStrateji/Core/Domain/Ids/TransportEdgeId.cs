using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Ulaşım grafı kenar kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct TransportEdgeId : IStableId, IEquatable<TransportEdgeId>, IComparable<TransportEdgeId>
    {
        public static readonly TransportEdgeId None = new TransportEdgeId(0UL);

        private readonly ulong _value;

        public TransportEdgeId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(TransportEdgeId a, TransportEdgeId b) => a._value == b._value;

        public static bool operator !=(TransportEdgeId a, TransportEdgeId b) => a._value != b._value;

        public static bool operator <(TransportEdgeId a, TransportEdgeId b) => a._value < b._value;

        public static bool operator >(TransportEdgeId a, TransportEdgeId b) => a._value > b._value;

        public static bool operator <=(TransportEdgeId a, TransportEdgeId b) => a._value <= b._value;

        public static bool operator >=(TransportEdgeId a, TransportEdgeId b) => a._value >= b._value;

        public bool Equals(TransportEdgeId other) => _value == other._value;

        public override bool Equals(object obj) => obj is TransportEdgeId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(TransportEdgeId other) => _value.CompareTo(other._value);

        public override string ToString() => "TransportEdgeId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
