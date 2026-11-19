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

        // Adım 4–6: durum şemasına nüfus/ekonomi, araştırma/siyaset/karakter, sonra kara harbi ve savaş
        // kaydı bölümleri eklendi.
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        public const string KernelDay60 = "63d3af6c4cf64e3b3ac34cc7120757b4694c6b53c9536144559bdc740f28b214"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "f9edf08c70282be8cd920460b2865b4e23778229d13cd09613a0d5c6b2b8b3f2"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "f1475b2a25c17379f8126f636b78df4b908d31df8abf55cdc4e69d0870d2e6ac"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Kara harbi kampanyası (toplama, hareket, muharebe, işgal) 60. gün durum hash'i.</summary>
        public const string WarDay60 = "0b988e178104810eedb6a803b7a323144b54e15298d6020f565fc0b6b66b2204"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır
    }
}
