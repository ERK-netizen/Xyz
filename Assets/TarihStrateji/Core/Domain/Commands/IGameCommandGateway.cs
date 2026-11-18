namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanının (MonoBehaviour) ve YZ'nin emir gönderdiği tek kapı
    /// (CLAUDE.md §2.1, §2.5). Uygulaması SimulationKernel'dedir.
    /// </summary>
    public interface IGameCommandGateway
    {
        /// <summary>
        /// Emri durumu değiştirmeden doğrular (önizleme, buton etkinliği). Aynı durumda
        /// aynı emir için her zaman aynı sonucu verir.
        /// </summary>
        ValidationResult Validate(GameCommand command);

        /// <summary>
        /// Emri doğrular; geçerliyse emir günlüğüne işler ve makbuz döner. Gün içi taktik
        /// saldırılar burada AP harcayarak anında çözülür ve <c>ResolutionId</c> ile
        /// işaretlenir; günlük fazlar onları ikinci kez uygulamaz.
        /// </summary>
        CommandReceipt Submit(GameCommand command);
    }
}
