using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Temel YZ ayarları. Haftalık karar; savaş ilanı için kendi gücü, hedefin tahmini gücünün
    /// 1,6 katı ve en az 3 tümen eşdeğeri, istikrar ≥ 0,45. Görülmeyen düşman şehri başına 1 tümen
    /// varsayılır. Saldırı için yerel güç, tahmini savunmanın 1,2 katı ve teşkilat ≥ %50. Barış
    /// en erken 60 gün sonra; 240 günde çıkmaz savaşta beyaz barış; şehirlerin %60'ından azı elde
    /// kaldıysa veya güç düşmanın 0,6'sının altındaysa "kaybediyoruz". Ordu hedefi barışta şehir
    /// başına 2 tümen, savaşta düşman tahmininin 1,2 katı; seferber nüfus çalışma çağının %12'sini
    /// aşmaz. Hazine 15 000'in üstündeyse aynı anda en çok 2 inşaat: önce silah, çelik, maden, tarım.
    /// Maliye: hazine 40 000'in üstündeyse vergi 0,02 düşer (en az 0,04), 10 000'in altındaysa artar.
    /// </summary>
    public static class BaselineAiContent
    {
        public static AiParameters Create() => new AiParameters(
            decisionIntervalDays: 7,
            aggression: Fixed64.One,
            declareWarPowerRatio: F("1.6"),
            minimumWarPower: F("3"),
            declareWarMinStability: F("0.45"),
            garrisonEstimatePerCity: Fixed64.One,
            attackPowerRatio: F("1.0"),
            attackMinOrganization: F("0.5"),
            defendRadius: 150_000L,
            advanceSearchRadius: 600_000L,
            unitsPerAdvanceTarget: 2,
            peaceAfterWarDays: 60,
            stalemateDays: 240,
            losingHeldShare: F("0.6"),
            losingPowerRatio: F("0.6"),
            recruitPerCity: F("2"),
            recruitThreatMargin: F("1.2"),
            recruitUnitType: BaselineUnits.InfantryDivision,
            maxMobilizationShare: F("0.12"),
            constructionReserve: F("15000"),
            maxConcurrentConstructions: 2,
            buildPriority: new[] { BaselineFacilities.ArmsFactory, BaselineFacilities.SteelWorks, BaselineFacilities.CoalMine, BaselineFacilities.IronMine, BaselineFacilities.Farm, BaselineFacilities.ConsumerFactory },
            treasuryHigh: F("80000"),
            treasuryLow: F("25000"),
            taxStep: F("0.02"),
            minTaxRate: F("0.04"),
            maxTaxRate: F("0.4"));

        private static Fixed64 F(string text) => Fixed64.Parse(text);
    }
}
