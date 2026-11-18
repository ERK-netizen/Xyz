using System;
using System.Globalization;

namespace TarihStrateji.Core.World.Grid
{
    /// <summary>"odd-r" ofset koordinatı: tek satırlar yarım hücre doğuya kaymıştır.</summary>
    [Serializable]
    public readonly struct HexCoord : IEquatable<HexCoord>, IComparable<HexCoord>
    {
        public HexCoord(int column, int row)
        {
            Column = column;
            Row = row;
        }

        public int Column { get; }

        public int Row { get; }

        public bool IsOddRow => (Row & 1) != 0;

        public static bool operator ==(HexCoord a, HexCoord b) => a.Column == b.Column && a.Row == b.Row;

        public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

        public bool Equals(HexCoord other) => this == other;

        public override bool Equals(object obj) => obj is HexCoord other && this == other;

        public override int GetHashCode() => (Row * 397) ^ Column;

        /// <summary>Satır, sonra sütun: <see cref="HexGrid"/> kimlik sırasıyla aynıdır.</summary>
        public int CompareTo(HexCoord other)
        {
            int byRow = Row.CompareTo(other.Row);
            return byRow != 0 ? byRow : Column.CompareTo(other.Column);
        }

        public override string ToString()
        {
            return "(" + Column.ToString(CultureInfo.InvariantCulture) + "," + Row.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }
}
