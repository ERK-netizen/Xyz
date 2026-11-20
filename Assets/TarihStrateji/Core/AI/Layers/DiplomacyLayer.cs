using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.AI.Layers
{
    /// <summary>
    /// Diplomatik katman (karar günlerinde):
    /// <list type="number">
    /// <item>Gelen teklifler: barışı kaybediyorsak her koşulda; toprak vermiyorsak ve savaş
    /// <see cref="AiParameters.PeaceAfterWarDays"/>'i aştıysa kabul et. Saldırmazlığı, o ülkeye
    /// savaş düşünmüyorsak; ittifakı ortak düşman varsa kabul et.</item>
    /// <item>Barış önerisi: işgalle desteklenen bölgeler varsa onları iste; kaybediyorsak veya
    /// savaş <see cref="AiParameters.StalemateDays"/>'i aştıysa beyaz barış öner.</item>
    /// <item>İttifak: ortak düşmanla savaşan komşuya ittifak öner.</item>
    /// <item>Savaş ilanı (<see cref="AiParameters.Aggression"/> &gt; 0): barıştaysak, istikrar yeterli,
    /// en zayıf komşuya karşı güç oranı eşiği aşılıyorsa.</item>
    /// </list>
    /// </summary>
    public static class DiplomacyLayer
    {
        public static void Decide(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            RespondToProposals(p, prm, a, intents);
            ProposePeace(p, prm, a, intents);
            SeekAllies(p, a, intents);
            ConsiderWar(p, prm, a, intents);
        }

        private static void RespondToProposals(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            for (int i = 0; i < p.Diplomacy.Proposals.Count; i++)
            {
                Proposal proposal = p.Diplomacy.Proposals.ValueAt(i);
                if (proposal.Status != ProposalStatus.Pending || proposal.To != p.Country || p.Today > proposal.LastDay)
                {
                    continue;
                }

                bool accept;
                switch (proposal.Kind)
                {
                    case ProposalKind.Peace:
                        accept = a.IsLosing() || (!CedesTerritory(p, proposal) && WarDays(p, proposal.From) >= prm.PeaceAfterWarDays && !IsWinning(p, a, proposal.From));
                        break;
                    case ProposalKind.NonAggression:
                        accept = !WouldAttack(p, prm, a, proposal.From);
                        break;
                    default:
                        accept = SharesEnemy(p, proposal.From);
                        break;
                }

                intents.Add(AiIntent.Respond(proposal.Id, accept));
            }
        }

        private static void ProposePeace(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            for (int e = 0; e < a.Enemies.Count; e++)
            {
                CountryId enemy = a.Enemies[e];
                if (WarDays(p, enemy) < prm.PeaceAfterWarDays || HasPendingPeace(p, enemy))
                {
                    continue;
                }

                RegionTransfer[] demands = OccupiedDemands(p, enemy);
                if (demands.Length > 0)
                {
                    intents.Add(AiIntent.Peace(enemy, demands));
                }
                else if (a.IsLosing() || WarDays(p, enemy) >= prm.StalemateDays)
                {
                    intents.Add(AiIntent.Peace(enemy, new RegionTransfer[0]));
                }
            }
        }

        private static void SeekAllies(AiPerception p, StrategicAssessment a, List<AiIntent> intents)
        {
            if (!a.AtWar)
            {
                return;
            }

            for (int n = 0; n < p.Neighbors.Count; n++)
            {
                CountryId other = p.Neighbors[n];
                if (!p.Diplomacy.AtWar(p.Country, other) && !p.Diplomacy.HasTreaty(TreatyKind.Alliance, p.Country, other, p.Today)
                    && SharesEnemy(p, other) && !HasPending(p, ProposalKind.Alliance, other))
                {
                    intents.Add(AiIntent.Alliance(other));
                }
            }
        }

        private static void ConsiderWar(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            if (!prm.Aggression.IsPositive || a.AtWar || p.Today < p.EarliestWar || p.Stability < prm.DeclareWarMinStability || a.OwnPower < prm.MinimumWarPower)
            {
                return;
            }

            CountryId best = CountryId.None;
            Fixed64 bestPower = Fixed64.MaxValue;
            for (int n = 0; n < p.Neighbors.Count; n++)
            {
                CountryId other = p.Neighbors[n];
                if (!CanAttack(p, other))
                {
                    continue;
                }

                Fixed64 power = a.EstimatedPower(other);
                if (power < bestPower)
                {
                    bestPower = power;
                    best = other;
                }
            }

            // Saldırganlık eşiği ölçekler: 1 → oran olduğu gibi; 0,5 → iki kat üstünlük gerekir.
            if (best.IsValid && a.OwnPower * prm.Aggression >= bestPower * prm.DeclareWarPowerRatio)
            {
                intents.Add(AiIntent.War(best));
            }
        }

        private static bool CanAttack(AiPerception p, CountryId other)
        {
            DiplomacyState d = p.Diplomacy;
            return !d.HasTreaty(TreatyKind.NonAggression, p.Country, other, p.Today) && !d.HasTreaty(TreatyKind.Alliance, p.Country, other, p.Today)
                && !d.HasTreaty(TreatyKind.Peace, p.Country, other, p.Today) && !d.IsExiled(other);
        }

        private static bool WouldAttack(AiPerception p, AiParameters prm, StrategicAssessment a, CountryId other)
        {
            return prm.Aggression.IsPositive && a.OwnPower * prm.Aggression >= a.EstimatedPower(other) * prm.DeclareWarPowerRatio;
        }

        private static bool IsWinning(AiPerception p, StrategicAssessment a, CountryId enemy)
        {
            return OccupiedDemands(p, enemy).Length > 0 || (a.OwnPower > a.EstimatedPower(enemy) * 2 && a.HeldCityShare == Fixed64.One);
        }

        private static bool CedesTerritory(AiPerception p, Proposal proposal)
        {
            for (int i = 0; i < proposal.Transfers.Count; i++)
            {
                if (proposal.Transfers[i].NewOwner != p.Country)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Düşmana ait olup hücrelerinin eşiğin üstünde kısmını kontrol ettiğimiz bölgeler (kimlik sırasıyla).</summary>
        public static RegionTransfer[] OccupiedDemands(AiPerception p, CountryId enemy)
        {
            var list = new List<RegionTransfer>();
            for (int r = 0; r < p.World.Regions.Count; r++)
            {
                p.World.TryGetControl(p.World.Regions[r], out RegionControl region);
                if (region.Owner == enemy && DiplomacyRules.ControlShare(p.World, region.Region, p.Country) > p.DiplomacyParameters.DemandControlShare)
                {
                    list.Add(new RegionTransfer(region.Region, p.Country));
                }
            }

            return list.ToArray();
        }

        private static int WarDays(AiPerception p, CountryId enemy)
        {
            return p.Diplomacy.TryGetWar(p.Country, enemy, out War war) ? p.Today.DayNumber - war.Start.DayNumber : 0;
        }

        private static bool SharesEnemy(AiPerception p, CountryId other)
        {
            for (int i = 0; i < p.Diplomacy.Wars.Count; i++)
            {
                War w = p.Diplomacy.Wars.ValueAt(i);
                if (w.Involves(p.Country) && p.Diplomacy.AtWar(other, w.Opponent(p.Country)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPendingPeace(AiPerception p, CountryId enemy)
        {
            for (int i = 0; i < p.Diplomacy.Proposals.Count; i++)
            {
                Proposal x = p.Diplomacy.Proposals.ValueAt(i);
                if (x.Status == ProposalStatus.Pending && x.Kind == ProposalKind.Peace && ((x.From == p.Country && x.To == enemy) || (x.From == enemy && x.To == p.Country)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPending(AiPerception p, ProposalKind kind, CountryId other)
        {
            for (int i = 0; i < p.Diplomacy.Proposals.Count; i++)
            {
                Proposal x = p.Diplomacy.Proposals.ValueAt(i);
                if (x.Status == ProposalStatus.Pending && x.Kind == kind && ((x.From == p.Country && x.To == other) || (x.From == other && x.To == p.Country)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
