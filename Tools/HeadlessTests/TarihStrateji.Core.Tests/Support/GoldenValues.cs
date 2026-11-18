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

        // Adım 4–5: durum şemasına nüfus/ekonomi, sonra araştırma/siyaset/karakter bölümleri eklendi.
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        public const string KernelDay60 = "ecad9f1df53196e7c9a4e4a9db4af314aa45abdd56b99dcff96625fc348ac62b"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "0690b7930918b963c930b178d9f18ce77366b10291307f6cfe515624affe7eb6"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "3216d3639b67129e8368e5d9df8eaa8db608d365ccfed500cffcd0f591eeeb9a"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır
    }
}
