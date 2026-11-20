using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// İş gücü ataması (P7): her bölgenin iş gücü (seferberlik düşülmüş) o bölgedeki çalışır
    /// tesislere tesis kimliği sırasıyla dağıtılır. İşgal altındaki tesise işçi verilmez.
    /// </summary>
    public static class WorkforceRules
    {
        public static void AssignDay(EconomyState economy, PopulationState population, GeoWorld world, EconomyCatalog catalog, DemographyParameters demography)
        {
            var remaining = new StableIdMap<RegionId, long>(population.Regions.Count);
            var labor = new StableIdMap<RegionId, long>(population.Regions.Count);
            for (int i = 0; i < population.Regions.Count; i++)
            {
                RegionPopulation p = population.Regions.ValueAt(i);
                long force = p.LaborForce(demography.LaborParticipation);
                remaining.Add(p.Region, force);
                labor.Add(p.Region, force);
            }

            for (int i = 0; i < economy.Facilities.Count; i++)
            {
                Facility facility = economy.Facilities.ValueAt(i);
                facility.Workers = 0L;
                if (!EconomyGeography.IsOperational(world, facility.City, facility.Owner))
                {
                    continue;
                }

                RegionId region = EconomyGeography.RegionOfCity(world, facility.City);
                if (!remaining.TryGetValue(region, out long available) || available <= 0L)
                {
                    continue;
                }

                long required = catalog.FacilityTypes[facility.Type].WorkersRequired;
                long assigned = required < available ? required : available;
                facility.Workers = assigned;
                remaining[region] = available - assigned;
            }

            for (int i = 0; i < population.Regions.Count; i++)
            {
                RegionPopulation p = population.Regions.ValueAt(i);
                PopulationRules.SetEmployed(p, labor.ValueAt(i) - remaining.ValueAt(i), demography);
            }
        }
    }
}
