using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>
    /// Kurulu tesis (düz veri). Bir şehirde yapı slotu kullanır. Şehrin fiilî kontrolcüsü
    /// sahip değilse (işgal) tesis çalışmaz.
    /// </summary>
    public sealed class Facility
    {
        internal Facility(FacilityId id, FacilityTypeId type, CityId city, CountryId owner)
        {
            Id = id;
            Type = type;
            City = city;
            Owner = owner;
        }

        public FacilityId Id { get; }

        public FacilityTypeId Type { get; }

        public CityId City { get; }

        public CountryId Owner { get; }

        /// <summary>Bugün atanan işçi (P7).</summary>
        public long Workers { get; internal set; }

        /// <summary>Bugünkü üretim ölçeği (P8): kadro × verim, girdi kısıtlı.</summary>
        public Fixed64 OutputScale { get; internal set; }
    }
}
