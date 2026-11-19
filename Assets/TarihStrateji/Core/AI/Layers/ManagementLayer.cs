using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.AI.Layers
{
    /// <summary>
    /// Yönetim katmanı (karar günlerinde): boştaysa ilk uygun teknoloji ve odak; bekleyen olaylarda
    /// ilk seçenek; ordu hedefin altındaysa ve seferberlik tavanı izin veriyorsa karar günü başına bir
    /// birlik toplama; hazine yedeğin üstündeyse bir tesis yatırımı; hazine bandının dışındaysa
    /// vergi bir adım (kaçak hazineye karşı dengeleyici). Ordu hedefi: barışta
    /// şehir × <see cref="AiParameters.RecruitPerCity"/>, savaşta ayrıca düşman tahmini gücü ×
    /// <see cref="AiParameters.RecruitThreatMargin"/>.
    /// </summary>
    public static class ManagementLayer
    {
        public static void Decide(AiPerception p, AiParameters prm, StrategicAssessment a, List<AiIntent> intents)
        {
            if (!p.CurrentResearch.IsValid && p.AvailableTechnologies.Count > 0)
            {
                intents.Add(AiIntent.Research(p.AvailableTechnologies[0]));
            }

            if (!p.CurrentFocus.IsValid && p.AvailableFocuses.Count > 0)
            {
                intents.Add(AiIntent.Focus(p.AvailableFocuses[0]));
            }

            for (int i = 0; i < p.PendingEvents.Count; i++)
            {
                intents.Add(AiIntent.Event(p.PendingEvents[i], 0));
            }

            Fixed64 desired = prm.RecruitPerCity * a.OwnedCities;
            if (a.AtWar)
            {
                desired = Fixed64.Max(desired, a.EnemyPower() * prm.RecruitThreatMargin);
            }

            long recruitMen = p.Warfare.UnitTypes[prm.RecruitUnitType].Manpower;
            bool withinMobilization = Fixed64.FromInt(p.Mobilized + recruitMen) <= Fixed64.FromInt(p.WorkingAge) * prm.MaxMobilizationShare;
            if (p.RecruitSites.Count > 0 && a.OwnPower < desired && withinMobilization)
            {
                intents.Add(AiIntent.Recruit(prm.RecruitUnitType, p.RecruitSites[0]));
            }

            if (p.Treasury > prm.TreasuryHigh && p.TaxRate > prm.MinTaxRate)
            {
                intents.Add(AiIntent.Tax(Fixed64.Max(prm.MinTaxRate, p.TaxRate - prm.TaxStep)));
            }
            else if (p.Treasury < prm.TreasuryLow && p.TaxRate < prm.MaxTaxRate)
            {
                intents.Add(AiIntent.Tax(Fixed64.Min(prm.MaxTaxRate, p.TaxRate + prm.TaxStep)));
            }

            if (p.BuildOptions.Count > 0 && p.Treasury >= prm.ConstructionReserve && p.ConstructionsInProgress < prm.MaxConcurrentConstructions)
            {
                intents.Add(AiIntent.Build(p.BuildOptions[0].FacilityType, p.BuildOptions[0].Sector));
            }
        }
    }
}
