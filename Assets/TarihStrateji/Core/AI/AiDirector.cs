using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Layers;
using TarihStrateji.Core.AI.Perception;

namespace TarihStrateji.Core.AI
{
    /// <summary>
    /// Katmanlı YZ'nin giriş noktası. Stratejik değerlendirme her gün kurulur; diplomasi ve yönetim
    /// katmanları ülke kimliğine göre kaydırılmış karar günlerinde
    /// (<c>(gün + ülke) mod aralık == 0</c>), ordu katmanı her gün çalışır. Saf ve deterministiktir:
    /// rastgelelik yok, eşitlikler kimlik sırasıyla bozulur; aynı algı aynı kararları üretir.
    /// </summary>
    public static class AiDirector
    {
        public static bool IsDecisionDay(AiPerception p, AiParameters prm)
        {
            long key = p.Today.DayNumber + (long)p.Country.Value;
            return key % prm.DecisionIntervalDays == 0;
        }

        public static void Decide(AiPerception p, AiParameters prm, List<AiIntent> intents)
        {
            var assessment = new StrategicAssessment(p, prm);
            if (IsDecisionDay(p, prm))
            {
                DiplomacyLayer.Decide(p, prm, assessment, intents);
                ManagementLayer.Decide(p, prm, assessment, intents);
            }

            ArmyLayer.Decide(p, prm, assessment, intents);
        }
    }
}
