using System.Collections.Generic;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.World;

namespace TarihStrateji.Core.Intelligence
{
    /// <summary>İstihbarat değişmezleri: temaslar sıralı ve yabancı, tahminler [0, 1], görünür hücreler sıralı.</summary>
    public static class IntelligenceInvariants
    {
        public static int Check(IntelligenceState state, WorldMap world, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Countries.Count; i++)
            {
                ObservedWorld view = state.Countries.ValueAt(i);
                for (int c = 0; c < view.Contacts.Count; c++)
                {
                    ContactReport r = view.Contacts[c];
                    if (r.Owner == view.Country || r.EstimatedStrength.IsNegative || r.EstimatedStrength > Fixed64.One
                        || (c > 0 && view.Contacts[c - 1].SortKey >= r.SortKey) || !world.Grid.TryFromId(r.LastHex, out _))
                    {
                        violations.Add(IntelligenceErrorKeys.DurumTutarsiz + " @ temas");
                    }
                }

                for (int v = 1; v < view.VisibleCells.Count; v++)
                {
                    if (view.VisibleCells[v - 1] >= view.VisibleCells[v])
                    {
                        violations.Add(IntelligenceErrorKeys.DurumTutarsiz + " @ gorus");
                    }
                }
            }

            return violations.Count - before;
        }
    }
}
