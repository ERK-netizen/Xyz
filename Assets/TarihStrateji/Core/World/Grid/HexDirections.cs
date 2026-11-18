using System;

namespace TarihStrateji.Core.World.Grid
{
    /// <summary><see cref="HexDirection"/> yardımcıları.</summary>
    public static class HexDirections
    {
        public const int Count = 6;

        public static HexDirection Opposite(HexDirection direction)
        {
            return (HexDirection)(((int)direction + 3) % Count);
        }

        /// <summary>Saat yönünün tersine (doğu → kuzeydoğu …) bir adım.</summary>
        public static HexDirection RotateCounterClockwise(HexDirection direction, int steps)
        {
            int value = ((int)direction + steps) % Count;
            return (HexDirection)(value < 0 ? value + Count : value);
        }

        /// <summary>Yön kuzeye veya güneye satır değiştiriyor mu (çapraz komşu).</summary>
        public static bool ChangesRow(HexDirection direction)
        {
            return direction != HexDirection.East && direction != HexDirection.West;
        }

        public static HexDirection FromIndex(int index)
        {
            if ((uint)index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (HexDirection)index;
        }
    }
}
