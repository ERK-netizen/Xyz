namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunumun oturuma tek erişim noktası (Bootstrap uygular): okuma modeli, emir kapısı, emir
    /// üretici ve oyun hızı. Sunum bunu Unity'de arayüz üzerinden bulur; derlemeler birbirine
    /// başvurmaz.
    /// </summary>
    public interface IGameSession
    {
        IGameReadModel ReadModel { get; }

        IGameCommandGateway Commands { get; }

        IGameCommandFactory Orders { get; }

        /// <summary>Saniyedeki oyun günü (0 = duraklatıldı).</summary>
        int DaysPerSecond { get; }

        void SetDaysPerSecond(int days);

        /// <summary>
        /// Zamanı <paramref name="days"/> gün ileri alır (+1/+7/+30): günler sıradaki karelerde, karede en çok
        /// birkaç gün olarak işlenir; oyun hızından bağımsızdır, duraklatılmışken de çalışır. Simülasyon durursa
        /// bekleyen günler bırakılır.
        /// </summary>
        void SkipDays(int days);

        /// <summary>İşlenmeyi bekleyen gün sayısı (0 = boşta).</summary>
        int PendingDays { get; }
    }
}
