using System;
using System.Globalization;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>Diplomatik teklif kimliği. 0 = <see cref="None"/>.</summary>
    [Serializable]
    public readonly struct ProposalId : IStableId, IEquatable<ProposalId>, IComparable<ProposalId>
    {
        public static readonly ProposalId None = new ProposalId(0UL);

        private readonly ulong _value;

        public ProposalId(ulong value)
        {
            _value = value;
        }

        public ulong Value => _value;

        public bool IsValid => _value != 0UL;

        public static bool operator ==(ProposalId a, ProposalId b) => a._value == b._value;

        public static bool operator !=(ProposalId a, ProposalId b) => a._value != b._value;

        public static bool operator <(ProposalId a, ProposalId b) => a._value < b._value;

        public static bool operator >(ProposalId a, ProposalId b) => a._value > b._value;

        public static bool operator <=(ProposalId a, ProposalId b) => a._value <= b._value;

        public static bool operator >=(ProposalId a, ProposalId b) => a._value >= b._value;

        public bool Equals(ProposalId other) => _value == other._value;

        public override bool Equals(object obj) => obj is ProposalId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo(ProposalId other) => _value.CompareTo(other._value);

        public override string ToString() => "ProposalId(" + _value.ToString(CultureInfo.InvariantCulture) + ")";
    }
}
