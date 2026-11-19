using System.Collections.Generic;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Olay/odak değişmezleri: tanımlı kimlikler, sıralı bekleyenler, geçerli odak ilerlemesi.</summary>
    public static class NarrativeInvariants
    {
        public static int Check(NarrativeState state, NarrativeCatalog catalog, List<string> violations)
        {
            int before = violations.Count;
            for (int c = 0; c < state.Countries.Count; c++)
            {
                CountryNarrative n = state.Countries.ValueAt(c);
                for (int i = 0; i < n.Pending.Count; i++)
                {
                    if (!catalog.Events.ContainsKey(n.Pending[i].EventId) || (i > 0 && n.Pending[i - 1].EventId >= n.Pending[i].EventId)
                        || n.Pending[i].LastDay < n.Pending[i].FiredOn)
                    {
                        violations.Add(NarrativeErrorKeys.DurumTutarsiz + " @ olay");
                    }
                }

                if (n.CurrentFocus.IsValid && (!catalog.Focuses.TryGetValue(n.CurrentFocus, out FocusDefinition f)
                    || n.FocusProgressDays >= f.Days || n.CompletedFocuses.ContainsKey(n.CurrentFocus)))
                {
                    violations.Add(NarrativeErrorKeys.DurumTutarsiz + " @ odak");
                }

                if (!n.CurrentFocus.IsValid && n.FocusProgressDays != 0)
                {
                    violations.Add(NarrativeErrorKeys.DurumTutarsiz + " @ odak");
                }

                for (int i = 0; i < n.CompletedFocuses.Count; i++)
                {
                    if (!catalog.Focuses.ContainsKey(n.CompletedFocuses.KeyAt(i)))
                    {
                        violations.Add(NarrativeErrorKeys.DurumTutarsiz + " @ odak");
                    }
                }
            }

            return violations.Count - before;
        }
    }
}
