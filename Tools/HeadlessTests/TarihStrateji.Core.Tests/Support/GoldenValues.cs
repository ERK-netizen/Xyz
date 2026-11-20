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

        // Adım 4–7: durum şemasına nüfus/ekonomi, araştırma/siyaset/karakter, kara harbi ve savaş kaydı,
        // sonra filo/hava kanadı/abluka bölümleri ve kara birliğine "taşıyan filo" alanı eklendi.
        // Şema sürümü 1 → 2 → 3 (hash'lenen baytlarda yazılı) ve Adım 8 bölümleri (diplomasi, istihbarat,
        // olay/odak) tüm durum hash'lerini değiştirdi. Savaş senaryolarında ayrıca istihbarat temasları ve
        // "savaş yorgunluğu" olayı duruma girer; kara harbi sonuçları ölçümle birebir aynıdır.
        // Kurallar değişmedi: 120. gün hazine/nüfus/stok değerleri eski ve yeni kodda birebir aynı ölçüldü.
        // Adım 13 (ADR-011): altıgen model silindi. Şema 5: dünya = bölge çokgenleri + şehirler + ulaşım grafı,
        // tesis/inşaat şehirde, birlik/temas düğümde. Test dünyaları altıgensiz yeniden kurulduğu için tüm
        // durum hash'leri yeniden üretildi; aynı tohum → aynı hash ve kaydet/yükle sürekliliği aynı testlerde doğrulanır.
        /// <summary>"şemaSürümü:parmakİzi" (bkz. SchemaVersionTests). Değişirse şema sürümü de artar.</summary>
        public const string SchemaFingerprint = "5:075ecddb7133dbe4520434348d7748112b575dd0678970edd651f653c0189071";

        public const string KernelDay60 = "e20ec97171a177e1ff55ba32c2215559c32d7bac444dc1bbcdd0ca6f7d760439"; // PRNG ve SHA-256 ayrıca kâhinlerle doğrulanır

        public const string EconomyDay120 = "55fe1167f8847d0de7ed021a1dfb216d616ddd151b07e4d13843ed131e59a0e9"; // defter/korunum aynı testlerde doğrulanır

        public const string StatecraftDay200 = "43fab33b6c10965e1cd7de85bccc6436e1c4cd90037095d0d99c195f3096b3ac"; // defter, araştırma günü ve kaydet/yükle aynı dosyada doğrulanır


        /// <summary>Diplomasi + odak + olay senaryosu (teklif düşmesi, iki taraflı barış, geri çekilme) 60. gün durum hash'i.</summary>
        public const string DiplomacyDay60 = "1e0fc84344d65c056d80bc99f2e63597a12d0996b216d301a76db5edafa8b785"; // aynı tohum ve kaydet/yükle aynı dosyada doğrulanır

        /// <summary>365 günlük "Ege 1925" YZ kampanyası (Gözlemci modu, savaşla başlar, tohum 7) son gün durum hash'i.</summary>
        public const string CampaignDay365 = "501d5b310b16a5110f8e861bc718d5d4846065dc9538d6f8dc0b534bb23d6972"; // aynı kampanya ve 180. gün kaydet/yükle aynı testte doğrulanır

        /// <summary>"1914 MVP" YZ'li Gözlemci koşusu (tohum 11) 180. gün durum hash'i; paket ve senaryo kurulumu dahil.</summary>
        public const string Scenario1914Day180 = "ece82e9064306859d74dcc7885a2b61d3603cc5db085608bf9d4770ad240018d"; // aynı tohum ve 90. gün kaydet/yükle aynı testte doğrulanır
    }
}
