using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.World.Grid
{
    /// <summary>
    /// Tek kanonik dünya ızgarası (CLAUDE.md §2.3): <see cref="Width"/> sütun × <see cref="Height"/>
    /// satır, sivri tepeli altıgenler, "odd-r" düzen. Sütunlar boylamda ±180°'de sarmalanır;
    /// satırlar kutuplarda sarmalanmaz. Hücre kimliği <c>row·Width + column + 1</c>'dir: harita
    /// boyutu sabit kaldıkça kararlıdır ve kimlik sırası satır-sütun sırasıdır.
    /// </summary>
    public sealed class HexGrid
    {
        public const int MinWidth = 4;
        public const int MinHeight = 2;
        public const int MaxCells = 4_000_000;

        private static readonly int[] EvenRowDeltaColumn = { 1, 0, -1, -1, -1, 0 };
        private static readonly int[] OddRowDeltaColumn = { 1, 1, 0, -1, 0, 1 };
        private static readonly int[] DeltaRow = { 0, -1, -1, 0, 1, 1 };

        private static readonly Fixed64 FullTurn = Fixed64.FromInt(360L);
        private static readonly Fixed64 HalfTurn = Fixed64.FromInt(180L);

        public HexGrid(int width, int height, Fixed64 northLatitude, Fixed64 southLatitude)
        {
            if (width < MinWidth || height < MinHeight || (long)width * height > MaxCells)
            {
                throw new ArgumentOutOfRangeException(nameof(width), WorldErrorKeys.IzgaraBoyutuGecersiz);
            }

            if (northLatitude <= southLatitude || northLatitude > Fixed64.FromInt(90L) || southLatitude < Fixed64.FromInt(-90L))
            {
                throw new ArgumentOutOfRangeException(nameof(northLatitude), WorldErrorKeys.EnlemAraligiGecersiz);
            }

            Width = width;
            Height = height;
            NorthLatitude = northLatitude;
            SouthLatitude = southLatitude;
            RowSpacingDegrees = (northLatitude - southLatitude) / height;
            ColumnSpacingDegrees = FullTurn / width;
        }

        public int Width { get; }

        public int Height { get; }

        public int CellCount => Width * Height;

        public Fixed64 NorthLatitude { get; }

        public Fixed64 SouthLatitude { get; }

        public Fixed64 RowSpacingDegrees { get; }

        public Fixed64 ColumnSpacingDegrees { get; }

        public bool Contains(HexCoord coord)
        {
            return coord.Row >= 0 && coord.Row < Height && coord.Column >= 0 && coord.Column < Width;
        }

        /// <summary>Sütunu ±180° dikişinde sarmalar; satır sınır dışıysa <c>false</c>.</summary>
        public bool TryWrap(int column, int row, out HexCoord coord)
        {
            if (row < 0 || row >= Height)
            {
                coord = default;
                return false;
            }

            int c = column % Width;
            coord = new HexCoord(c < 0 ? c + Width : c, row);
            return true;
        }

        public int IndexOf(HexCoord coord)
        {
            RequireContains(coord);
            return (coord.Row * Width) + coord.Column;
        }

        public HexCoord FromIndex(int index)
        {
            if ((uint)index >= (uint)CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), WorldErrorKeys.HucreDisinda);
            }

            return new HexCoord(index % Width, index / Width);
        }

        public HexId ToId(HexCoord coord)
        {
            return new HexId((ulong)IndexOf(coord) + 1UL);
        }

        public bool TryFromId(HexId id, out HexCoord coord)
        {
            if (!id.IsValid || id.Value > (ulong)CellCount)
            {
                coord = default;
                return false;
            }

            coord = FromIndex((int)(id.Value - 1UL));
            return true;
        }

        public HexCoord FromId(HexId id)
        {
            if (!TryFromId(id, out HexCoord coord))
            {
                throw new ArgumentOutOfRangeException(nameof(id), WorldErrorKeys.HucreBulunamadi);
            }

            return coord;
        }

        /// <summary>Komşu; kutup satırının dışına çıkıyorsa <c>false</c>. Doğu-batı sarmalanır.</summary>
        public bool TryGetNeighbor(HexCoord coord, HexDirection direction, out HexCoord neighbor)
        {
            RequireContains(coord);
            int d = (int)direction;
            int dc = coord.IsOddRow ? OddRowDeltaColumn[d] : EvenRowDeltaColumn[d];
            return TryWrap(coord.Column + dc, coord.Row + DeltaRow[d], out neighbor);
        }

        /// <summary>Hücre merkezinin enlemi (derece).</summary>
        public Fixed64 CenterLatitude(int row)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row), WorldErrorKeys.HucreDisinda);
            }

            return NorthLatitude - Fixed64.MulDiv(Fixed64.FromInt((2L * row) + 1L), NorthLatitude - SouthLatitude, Fixed64.FromInt(2L * Height));
        }

        /// <summary>Hücre merkezinin boylamı, [-180, 180).</summary>
        public Fixed64 CenterLongitude(HexCoord coord)
        {
            RequireContains(coord);
            long halfSteps = (2L * coord.Column) + 1L + (coord.IsOddRow ? 1L : 0L);
            Fixed64 lon = Fixed64.MulDiv(Fixed64.FromInt(halfSteps), FullTurn, Fixed64.FromInt(2L * Width)) - HalfTurn;
            return lon >= HalfTurn ? lon - FullTurn : lon;
        }

        /// <summary>Dikiş dahil en kısa altıgen adım sayısı.</summary>
        public int StepDistance(HexCoord a, HexCoord b)
        {
            RequireContains(a);
            RequireContains(b);
            long aq = AxialQ(a);
            long bq = AxialQ(b);
            long dr = b.Row - a.Row;
            long best = long.MaxValue;
            for (int shift = -1; shift <= 1; shift++)
            {
                long dq = bq + ((long)shift * Width) - aq;
                long distance = (Abs(dq) + Abs(dr) + Abs(dq + dr)) / 2L;
                if (distance < best)
                {
                    best = distance;
                }
            }

            return (int)best;
        }

        private static long AxialQ(HexCoord c)
        {
            return c.Column - ((c.Row - (c.Row & 1)) / 2);
        }

        private static long Abs(long v) => v < 0L ? -v : v;

        private void RequireContains(HexCoord coord)
        {
            if (!Contains(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), WorldErrorKeys.HucreDisinda + " " + coord);
            }
        }
    }
}
