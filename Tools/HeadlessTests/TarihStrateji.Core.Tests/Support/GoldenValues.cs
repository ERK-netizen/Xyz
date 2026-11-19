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

        // Adım 4–7: durum şemasına nüfus/ekonomi, araştırma/siyaset/karakter, kara harbi ve savaş kaydı,
        // sonra filo/hava kanadı/abluka bölümleri ve kara birliğine "taşıyan filo" alanı eklendi.
        // Şema sürümü 1 → 2 (hash'lenen baytlarda yazılı) tüm durum hash'lerini bir kez daha değiştirdi.
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        /// <summary>"şemaSürümü:parmakİzi" (bkz. SchemaVersionTests). Değişirse şema sürümü de artar.</summary>
        public const string SchemaFingerprint = "2:bfe974742fde023486aa9799e221780c4b6fc6f7427182fec4c0d379f7e42ff4";

        public const string KernelDay60 = "9edf8889bf8c8e473ed80f5f676727716c28911b2aced10ac828f1896b97e170"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "4456e51be1888b68b94777fd461b43880d6ce999f4d6fa573a984d2dd28d9142"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "c12e3b8a9d0660c35380086507014b2e69263a23c3d3d1c88db74fe66a2bd897"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Kara harbi kampanyası (toplama, hareket, muharebe, işgal) 60. gün durum hash'i.</summary>
        /// <remarks>Adım 7'de kara kuralları değişmedi: kampanyanın birlik durumları ve 100 tohumluk 2:1 muharebe sonuçları birebir aynı ölçüldü.</remarks>
        public const string WarDay60 = "9975744bdb004f72603fae31d9520bc7df2bb07f518d2446f257051b5b3a1b38"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Ortak harekât (filo, gemiye binme, çıkarma, hava ve kıyı desteği, liman fethi, konvoy) 40. gün durum hash'i.</summary>
        public const string JointOperationDay40 = "7382deda256828aa5c8aba96cdbf355970d05feba9c0e405eb3412f682b243ee"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır
    }
}
