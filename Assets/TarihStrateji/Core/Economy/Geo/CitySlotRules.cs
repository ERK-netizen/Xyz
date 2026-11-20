using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Geo
{
    /// <summary>
    /// Sektörsüz şehirde yapı kapasitesi (§4.2, ADR-004 geçersiz): kapasite = gelişim seviyesi × seviye başına
    /// slot (denge verisi); altyapı (yol, demiryolu, liman bağlantısı) slot tüketmez. İnşaat yalnız şehrin
    /// sahibi ve fiilî kontrolcüsü aynı ülkeyken verilir; işgal altındaki şehirde inşaat ilerlemez.
    /// </summary>
    public static class CitySlotRules
    {
        public static int Capacity(City city, int slotsPerLevel) => city.BuildingSlots(slotsPerLevel);

        public static bool IsOperational(City city, CountryId country) => city.Owner == country && city.Controller == country;

        public static ValidationResult CheckBuild(City city, CountryId country, int usedSlots, int requiredSlots, int slotsPerLevel)
        {
            if (!IsOperational(city, country))
            {
                return ValidationResult.Fail(EconomyErrorKeys.SehirBizimDegil);
            }

            if (requiredSlots < 0 || usedSlots < 0 || usedSlots + requiredSlots > Capacity(city, slotsPerLevel))
            {
                return ValidationResult.Fail(EconomyErrorKeys.SlotYetersiz);
            }

            return ValidationResult.Ok;
        }
    }
}
