using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Politics.Definitions;
using TarihStrateji.Core.Politics.State;

namespace TarihStrateji.Core.Politics
{
    /// <summary>Siyaset değişmezleri: aralıklar, partiler ülkeye ait, iktidar destek listesinde.</summary>
    public static class PoliticsInvariants
    {
        public static int Check(PoliticsState state, PoliticsCatalog catalog, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryPolitics p = state.Countries.ValueAt(i);
                string at = " @ " + p.Country.Value.ToString(CultureInfo.InvariantCulture);
                if (!catalog.Forms.ContainsKey(p.Form) || p.Stability.IsNegative || p.Stability > Fixed64.One)
                {
                    violations.Add(PoliticsErrorKeys.DurumTutarsiz + at);
                }

                for (int k = 0; k < p.PartySupport.Count; k++)
                {
                    if (!catalog.Parties.TryGetValue(p.PartySupport.KeyAt(k), out PartyDefinition party) || party.Country != p.Country || !p.PartySupport.ValueAt(k).IsPositive)
                    {
                        violations.Add(PoliticsErrorKeys.PartiBuUlkeninDegil + at);
                    }
                }

                if (p.RulingParty.IsValid && !p.PartySupport.ContainsKey(p.RulingParty))
                {
                    violations.Add(PoliticsErrorKeys.PartiBilinmiyor + at);
                }

                for (int k = 0; k < p.Institutions.Count; k++)
                {
                    Fixed64 s = p.Institutions.ValueAt(k);
                    if (!catalog.Institutions.ContainsKey(p.Institutions.KeyAt(k)) || s.IsNegative || s > Fixed64.One)
                    {
                        violations.Add(PoliticsErrorKeys.DurumTutarsiz + at);
                    }
                }

                for (int k = 0; k < p.ActiveCrises.Count; k++)
                {
                    if (!catalog.Crises.ContainsKey(p.ActiveCrises.KeyAt(k)))
                    {
                        violations.Add(PoliticsErrorKeys.DurumTutarsiz + at);
                    }
                }
            }

            return violations.Count - before;
        }
    }
}
