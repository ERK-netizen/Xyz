using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Research
{
    /// <summary>Araştırma değişmezleri: araştırılanların ön koşulları da araştırılmış; çağ ve proje tutarlı.</summary>
    public static class ResearchInvariants
    {
        public static int Check(ResearchState state, TechCatalog catalog, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryResearch r = state.Countries.ValueAt(i);
                string at = " @ " + r.Country.Value.ToString(CultureInfo.InvariantCulture);
                if (r.Era < 0 || r.Era >= catalog.Eras.Count)
                {
                    violations.Add(ResearchErrorKeys.DurumTutarsiz + at);
                    continue;
                }

                for (int t = 0; t < r.Researched.Count; t++)
                {
                    if (!catalog.Technologies.TryGetValue(r.Researched.KeyAt(t), out TechnologyDefinition tech))
                    {
                        violations.Add(ResearchErrorKeys.TeknolojiBilinmiyor + at);
                        continue;
                    }

                    for (int p = 0; p < tech.Prerequisites.Count; p++)
                    {
                        if (!r.Has(tech.Prerequisites[p]))
                        {
                            violations.Add(ResearchErrorKeys.OnkosulEksik + at);
                        }
                    }

                    if (tech.Era > r.Era)
                    {
                        violations.Add(ResearchErrorKeys.CagaUlasilmadi + at);
                    }
                }

                for (int e = 1; e <= r.Era; e++)
                {
                    IReadOnlyList<TechnologyId> gate = catalog.Eras[e].GateTechnologies;
                    for (int g = 0; g < gate.Count; g++)
                    {
                        if (!r.Has(gate[g]))
                        {
                            violations.Add(ResearchErrorKeys.DurumTutarsiz + at);
                        }
                    }
                }

                if (r.Current.IsValid && (r.Has(r.Current) || !catalog.Technologies.ContainsKey(r.Current)))
                {
                    violations.Add(ResearchErrorKeys.DurumTutarsiz + at);
                }

                for (int p = 0; p < r.Progress.Count; p++)
                {
                    if (r.Progress.ValueAt(p).IsNegative || r.Has(r.Progress.KeyAt(p)))
                    {
                        violations.Add(ResearchErrorKeys.DurumTutarsiz + at);
                    }
                }
            }

            return violations.Count - before;
        }
    }
}
