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
        // Şema sürümü 1 → 2 → 3 (hash'lenen baytlarda yazılı) ve Adım 8 bölümleri (diplomasi, istihbarat,
        // olay/odak) tüm durum hash'lerini değiştirdi. Savaş senaryolarında ayrıca istihbarat temasları ve
        // "savaş yorgunluğu" olayı duruma girer; kara harbi sonuçları ölçümle birebir aynıdır.
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        /// <summary>"şemaSürümü:parmakİzi" (bkz. SchemaVersionTests). Değişirse şema sürümü de artar.</summary>
        public const string SchemaFingerprint = "3:ac8856e12445b63b354c95a5c2c6bbc26dbf6388f6442d8a2cb037a3e98d14e1";

        public const string KernelDay60 = "597a14fff0a7f9ea41419e2ce59ce8e54043742fb4fc5e44279fd5d4a1088eeb"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "5e7d5d33e5d6badc661d451a0f520e211cc97740435b510e449f9360a5c78a1f"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "6266810906780a5203ea5bda2f7567e430dad245d47b91ce4d8f389b29f1d014"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Kara harbi kampanyası (toplama, hareket, muharebe, işgal) 60. gün durum hash'i.</summary>
        /// <remarks>Adım 7'de kara kuralları değişmedi: kampanyanın birlik durumları ve 100 tohumluk 2:1 muharebe sonuçları birebir aynı ölçüldü.</remarks>
        public const string WarDay60 = "65505628a35101774ee67fe6537080778697e4d3137be37adf0d5513c8151d2e"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Diplomasi + odak + olay senaryosu (teklif düşmesi, iki taraflı barış, geri çekilme) 60. gün durum hash'i.</summary>
        public const string DiplomacyDay60 = "9a7b7e92988853ec755daaa4d53699d4164b4a52b5f2299968768272c78283ad"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Ortak harekât (filo, gemiye binme, çıkarma, hava ve kıyı desteği, liman fethi, konvoy) 40. gün durum hash'i.</summary>
        public const string JointOperationDay40 = "f085fd622ca9a60877c49dbd6e64593957319878fa3fe40928e7649ffcfc90a8"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır
    }
}
