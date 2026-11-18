namespace TarihStrateji.Core.Domain
{
    /// <summary>
    /// Domain katmanının Türkçe yerelleştirme anahtarları. Kural kodu kullanıcıya metin
    /// döndürmez; sunum katmanı bu anahtarları yerelleştirme tablosundan çözer.
    /// </summary>
    public static class DomainErrorKeys
    {
        public const string SayiBicimiGecersiz = "hata.sayi.bicimGecersiz";
        public const string SayiTasmasi = "hata.sayi.tasma";
        public const string KimlikGecersiz = "hata.kimlik.gecersiz";
        public const string KimlikZatenVar = "hata.kimlik.zatenVar";
        public const string KimlikBulunamadi = "hata.kimlik.bulunamadi";
        public const string KimlikTukendi = "hata.kimlik.tukendi";
        public const string TarihGecersiz = "hata.tarih.gecersiz";
        public const string EmirBos = "hata.emir.bos";
        public const string EmirSahibiGecersiz = "hata.emir.sahibiGecersiz";
        public const string HileNormalModda = "hata.emir.hileNormalModda";
        public const string GozlemciEmirVeremez = "hata.emir.gozlemciEmirVeremez";
        public const string ModBilinmiyor = "hata.emir.modBilinmiyor";
    }
}
