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
        public const string SchemaFingerprint = "4:0bf36d17293802857976681f7c58933a1433409a284a320f805766e833bd346e";

        public const string KernelDay60 = "e4e5d3789ca7726aaaaa1827419e213f34126e1c77c2793571d5398c5fc5493e"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "42ad3a37144b64f0226743422019ac7d0431aa8dd2d64e6e40010ba9777d4e55"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "28093e452e6994546f9dd7d00fa796651ced0689cf5c6913cd1c7ed19f5eeb46"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Kara harbi kampanyası (toplama, hareket, muharebe, işgal) 60. gün durum hash'i.</summary>
        /// <remarks>Adım 7'de kara kuralları değişmedi: kampanyanın birlik durumları ve 100 tohumluk 2:1 muharebe sonuçları birebir aynı ölçüldü.</remarks>
        public const string WarDay60 = "f3df3ca1f531011dfef18d2cb236c6804d2bd1602063b3fcba10ca6e93bbcca1"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>Diplomasi + odak + olay senaryosu (teklif düşmesi, iki taraflı barış, geri çekilme) 60. gün durum hash'i.</summary>
        public const string DiplomacyDay60 = "e09db353e6653a2db71066ba329be712eda252085cdc641142544dc1a89be8ab"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>365 günlük dört ülkeli YZ kampanyası (Gözlemci modu, tohum 7) son gün durum hash'i.</summary>
        public const string CampaignDay365 = "5ea35fa3cd951ad86818c7740ba43bd39c9936e81a9e2bc98010b8768f59d2bc"; // aynı kampanya ve 180. gün kaydet/yükle aynı testte doğrulanır

        /// <summary>Ortak harekât (filo, gemiye binme, çıkarma, hava ve kıyı desteği, liman fethi, konvoy) 40. gün durum hash'i.</summary>
        public const string JointOperationDay40 = "e29400aebf79ac65e440ae0dc30fff4a3435712bc364a0eeabb6173d6a272149"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır
    }
}
