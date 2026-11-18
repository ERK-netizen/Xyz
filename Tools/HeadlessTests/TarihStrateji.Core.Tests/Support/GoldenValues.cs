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

        // Adım 4: durum şemasına nüfus ve ekonomi bölümleri eklendi (kurallar değişmedi) → yeni değer.
        public const string KernelDay60 = "469da00c4109169fe85afcd00c079411dbf2597e09272df0bbf6d45d30803b0e"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "4fea483a32500519b7400c9eec733f19fb0c767a5a57bd130d0965a10f371bbd"; // defter/korunum aynı testlerde doğrulanır
    }
}
