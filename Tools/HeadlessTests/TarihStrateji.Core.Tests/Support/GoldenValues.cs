namespace TarihStrateji.Core.Tests.Support
{
    /// <summary>
    /// Altın determinizm sabitleri. Bir değerin değişmesi, kayıt ve yeniden oynatma
    /// uyumluluğunu kıran bir semantik değişikliktir: yalnız bilinçli olarak ve commit
    /// açıklamasında gerekçesiyle güncellenir.
    /// </summary>
    public static class GoldenValues
    {
        public const ulong Fixed64Chain = 0x3CC2B9F2A0D61353UL; // Python tamsayı kâhiniyle bağımsız doğrulandı

        public const ulong WorldMapAndPaths = 0xF55FC836AFFB48B1UL; // maliyetler testte Dijkstra kâhiniyle doğrulanır

        public const string KernelDay60 = "4b49632aac955eb8f1a848f8d37481394a8a003cf6896d50845e74b3be2501ee"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır
    }
}
