using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Koşulları ülkenin gerçek durumuna göre değerlendirir (Simulation uygular).</summary>
    public interface ICountryConditions
    {
        bool Holds(CountryId country, Condition condition);
    }
}
