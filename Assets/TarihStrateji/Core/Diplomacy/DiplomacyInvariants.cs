using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Diplomacy
{
    /// <summary>Günlük diplomasi değişmezleri (P15).</summary>
    public static class DiplomacyInvariants
    {
        public static int Check(DiplomacyState state, GeoWorld world, GameDate today, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Wars.Count; i++)
            {
                War w = state.Wars.ValueAt(i);
                string at = " @ savas " + w.Id.Value.ToString(CultureInfo.InvariantCulture);
                if (!w.Attacker.IsValid || !w.Defender.IsValid || w.Attacker == w.Defender)
                {
                    violations.Add(DiplomacyErrorKeys.DurumTutarsiz + at);
                }

                for (int j = i + 1; j < state.Wars.Count; j++)
                {
                    War v = state.Wars.ValueAt(j);
                    if ((v.Attacker == w.Attacker && v.Defender == w.Defender) || (v.Attacker == w.Defender && v.Defender == w.Attacker))
                    {
                        violations.Add(DiplomacyErrorKeys.DurumTutarsiz + at + " (çift tekrar)");
                    }
                }

                if (state.HasTreaty(TreatyKind.Alliance, w.Attacker, w.Defender, today) || state.HasTreaty(TreatyKind.Peace, w.Attacker, w.Defender, today))
                {
                    violations.Add(DiplomacyErrorKeys.DurumTutarsiz + at + " (antlaşmalı taraflar savaşta)");
                }

                for (int g = 0; g < w.Goals.Count; g++)
                {
                    if (!world.TryGetControl(w.Goals[g], out _))
                    {
                        violations.Add(DiplomacyErrorKeys.DurumTutarsiz + at);
                    }
                }
            }

            for (int i = 0; i < state.Proposals.Count; i++)
            {
                Proposal p = state.Proposals.ValueAt(i);
                if (p.From == p.To || !p.From.IsValid || !p.To.IsValid || p.LastDay < p.Created || (p.Kind != ProposalKind.Peace && p.Transfers.Count > 0))
                {
                    violations.Add(DiplomacyErrorKeys.DurumTutarsiz + " @ teklif " + p.Id.Value.ToString(CultureInfo.InvariantCulture));
                }
            }

            for (int i = 0; i < state.Exiles.Count; i++)
            {
                if (state.Exiles.KeyAt(i) == state.Exiles.ValueAt(i))
                {
                    violations.Add(DiplomacyErrorKeys.DurumTutarsiz + " @ surgun");
                }
            }

            return violations.Count - before;
        }
    }
}
