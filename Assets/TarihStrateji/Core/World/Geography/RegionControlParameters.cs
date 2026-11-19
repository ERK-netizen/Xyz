namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Bölge kontrol devri süreleri (denge verisi; dönem kurallarıyla sürümlenir).</summary>
    public sealed class RegionControlParameters
    {
        public RegionControlParameters(int daysToOccupy, int daysToSecure)
        {
            DaysToOccupy = daysToOccupy < 1 ? 1 : daysToOccupy;
            DaysToSecure = daysToSecure < 1 ? 1 : daysToSecure;
        }

        /// <summary>Çekişen ülkenin bölgeyi ikmalli ve kesintisiz baskın tutması gereken gün.</summary>
        public int DaysToOccupy { get; }

        /// <summary>İşgal sonrası (veya geri alınan bölgede) güvenli sayılmak için kesintisiz tutma.</summary>
        public int DaysToSecure { get; }
    }
}
