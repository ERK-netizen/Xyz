using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>Tesis/inşaat konumu (şehir) → bölge eşlemesi ve şehir yapı slotu hesabı (ADR-011).</summary>
    public static class EconomyGeography
    {
        public static RegionId RegionOfCity(GeoWorld world, CityId cityId) =>
            world.TryGetCity(cityId, out City city) ? city.Region : RegionId.None;

        /// <summary>Tesis çalışabilir mi: şehir var ve fiilî kontrol tesisin sahibinde.</summary>
        public static bool IsOperational(GeoWorld world, CityId cityId, CountryId owner) =>
            world.TryGetCity(cityId, out City city) && city.Controller == owner;

        /// <summary>Şehirde tesislerin ve süren inşaatların kullandığı slot toplamı.</summary>
        public static int UsedSlots(EconomyState economy, EconomyCatalog catalog, CityId cityId)
        {
            int used = 0;
            for (int i = 0; i < economy.Facilities.Count; i++)
            {
                Facility f = economy.Facilities.ValueAt(i);
                if (f.City == cityId && catalog.FacilityTypes.TryGetValue(f.Type, out FacilityTypeDefinition type))
                {
                    used = checked(used + type.BuildingSlots);
                }
            }

            for (int i = 0; i < economy.Constructions.Count; i++)
            {
                ConstructionOrder o = economy.Constructions.ValueAt(i);
                if (o.City == cityId && catalog.FacilityTypes.TryGetValue(o.Type, out FacilityTypeDefinition type))
                {
                    used = checked(used + type.BuildingSlots);
                }
            }

            return used;
        }
    }
}
