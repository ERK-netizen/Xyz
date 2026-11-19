namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>Antlaşma türü. Değerler kayıt biçimine yazılır; asla yeniden numaralanmaz.</summary>
    public enum TreatyKind : byte
    {
        NonAggression = 1,
        Alliance = 2,

        /// <summary>Barış: süresi boyunca ateşkes (aynı çifte savaş açılamaz).</summary>
        Peace = 3,
    }
}
