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
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        public const string KernelDay60 = "28f7bd07205543287c42ea017100ae2af728e7b2485c5c9d173d48dc08bd0163"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "c1496c38987f2242073dc7a3d5aac467c774a57aa3b565bc561ca1a1ac6bcf84"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "87ed22bf20f0423bd85d8b6b515c5eb8bb3fd814a2e06d5052ea99457fddb1aa"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Kara harbi kampanyası (toplama, hareket, muharebe, işgal) 60. gün durum hash'i.</summary>
        /// <remarks>Adım 7'de kara kuralları değişmedi: kampanyanın birlik durumları ve 100 tohumluk 2:1 muharebe sonuçları birebir aynı ölçüldü.</remarks>
        public const string WarDay60 = "326c3d9021a73b3abc0dfa5105113bb3c55b61b88f8c8999c9023d27ae2ef1cc"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Ortak harekât (filo, gemiye binme, çıkarma, hava ve kıyı desteği, liman fethi, konvoy) 40. gün durum hash'i.</summary>
        public const string JointOperationDay40 = "941ea6e166079db47d4f6e0e77b4d7d82ef2ba37fd7ddae23a4f32b19888dabc"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır
    }
}
