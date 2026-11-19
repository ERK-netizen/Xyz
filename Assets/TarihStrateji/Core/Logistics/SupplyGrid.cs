using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Logistics
{
    /// <summary>
    /// Bir ülkenin günlük ikmal haritası: hücre dizini → ikmal düzeyi [0, 1]. Türetilmiş veridir
    /// (P11'de her gün yeniden hesaplanır, kayda girmez).
    /// </summary>
    public sealed class SupplyGrid
    {
        private readonly long[] _levelRaw;

        internal SupplyGrid(CountryId country, long[] levelRaw)
        {
            Country = country;
            _levelRaw = levelRaw ?? throw new ArgumentNullException(nameof(levelRaw));
        }

        public CountryId Country { get; }

        public int CellCount => _levelRaw.Length;

        public Fixed64 Level(int cellIndex) => Fixed64.FromRaw(_levelRaw[cellIndex]);
    }
}
