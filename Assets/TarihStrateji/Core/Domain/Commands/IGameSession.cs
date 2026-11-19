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
    }
}
