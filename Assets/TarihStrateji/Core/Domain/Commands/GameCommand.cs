using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanından ve YZ'den <see cref="IGameCommandGateway"/> üzerinden gönderilen
    /// emirlerin tabanı. Emir değişmez veridir; durumu yalnız SimulationKernel uygular.
    /// </summary>
    public abstract class GameCommand
    {
        protected GameCommand(CountryId issuer)
        {
            Issuer = issuer;
        }

        /// <summary>Emri veren ülke. Sandbox emirleri için <see cref="CountryId.None"/> olabilir.</summary>
        public CountryId Issuer { get; }

        /// <summary>Emrin gerektirdiği yetki.</summary>
        public abstract CommandAuthority Authority { get; }

        /// <summary>
        /// Emir günlüğünde (journal) ve kayıtta kullanılan kalıcı tür anahtarı,
        /// ör. <c>emir.kara.hareket</c>. Bir kez yayımlandıktan sonra değiştirilmez.
        /// </summary>
        public abstract string Kind { get; }
    }
}
