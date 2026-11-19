namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Olay ve odak hataları (Türkçe mesaj anahtarları).</summary>
    public static class NarrativeErrorKeys
    {
        public const string TanimGecersiz = "hata.olay.tanimGecersiz";
        public const string OdakDongusu = "hata.odak.onKosulDongusu";
        public const string OlayBulunamadi = "hata.olay.bekleyenOlayYok";
        public const string SecenekGecersiz = "hata.olay.secenekGecersiz";
        public const string OdakBilinmiyor = "hata.odak.bilinmiyor";
        public const string OdakTamamlandi = "hata.odak.zatenTamamlandi";
        public const string OdakSurmekte = "hata.odak.zatenSecili";
        public const string OnKosulEksik = "hata.odak.onKosulEksik";
        public const string Dislanmis = "hata.odak.dislayiciOdakTamamlandi";
        public const string KosulSaglanmiyor = "hata.odak.kosulSaglanmiyor";
        public const string UlkeyeAitDegil = "hata.odak.buUlkeyeAitDegil";
        public const string DurumTutarsiz = "hata.olay.durumTutarsiz";
    }
}
