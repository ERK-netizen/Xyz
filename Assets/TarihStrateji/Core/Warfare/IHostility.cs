using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Warfare
{
    /// <summary>
    /// İki ülke savaşta mı. Savaş durumunun sahibi Diplomacy'dir (Adım 8); Warfare yalnız bu
    /// sözleşmeyi okur.
    /// </summary>
    public interface IHostility
    {
        bool AtWar(CountryId a, CountryId b);
    }
}
