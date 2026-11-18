using System.Collections.Generic;
using System.Globalization;

namespace TarihStrateji.Core.Population
{
    /// <summary>Nüfus değişmezleri (P15): toplam = Σ kohort, negatif yok, seferber ≤ çalışma çağı.</summary>
    public static class PopulationInvariants
    {
        public static int Check(PopulationState state, DemographyParameters parameters, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Regions.Count; i++)
            {
                RegionPopulation p = state.Regions.ValueAt(i);
                string at = " @ " + p.Region.Value.ToString(CultureInfo.InvariantCulture);
                for (int b = 0; b < AgeBands.Count; b++)
                {
                    if (p.Cohorts[b] < 0L)
                    {
                        violations.Add(PopulationErrorKeys.NegatifNufus + at);
                    }
                }

                if (p.Total != p.SumCohorts())
                {
                    violations.Add(PopulationErrorKeys.ToplamTutarsiz + at);
                }

                if (p.Mobilized < 0L || p.Mobilized > p.WorkingAge)
                {
                    violations.Add(PopulationErrorKeys.SeferberlikTutarsiz + at);
                }

                if (p.Employed < 0L || p.Employed > p.LaborForce(parameters.LaborParticipation))
                {
                    violations.Add(PopulationErrorKeys.IstihdamTutarsiz + at);
                }
            }

            return violations.Count - before;
        }
    }
}
