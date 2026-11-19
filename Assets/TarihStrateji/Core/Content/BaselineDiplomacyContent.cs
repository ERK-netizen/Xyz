using System;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Intelligence.Definitions;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Warfare;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Temel diplomasi, istihbarat, olay ve odak verisi (1920'ler–30'lar).
    /// Teklif 30 gün, saldırmazlık 5 yıl, ittifak 10 yıl, barış sonrası ateşkes 1 yıl. Barışta bir
    /// bölge ancak hücrelerinin yarısından fazlası işgal altındaysa talep edilebilir; sahip olduğu
    /// şehirlerin dörtte birinden azını elinde tutan savaştaki ülke teslim olur (savaşan müttefiki
    /// varsa sürgüne gider). Görüş: birlik/filo/şehir 2 hücre, hava keşfi hedef çevresinde 1 hücre;
    /// 1 hücre içindeki temasın gücü kesin, ötesi %25'lik adımlarla tahmin; temas 30 gün hatırlanır.
    /// </summary>
    public static class BaselineDiplomacyContent
    {
        public static DiplomacyParameters CreateDiplomacy() => new DiplomacyParameters(
            proposalLifetimeDays: 30,
            nonAggressionDays: 1825,
            allianceDays: 3650,
            truceDays: 365,
            demandControlShare: F("0.5"),
            capitulationThreshold: F("0.25"));

        public static IntelligenceParameters CreateIntelligence() => new IntelligenceParameters(
            landUnitSight: 2,
            fleetSight: 2,
            citySight: 2,
            airReconSight: 1,
            exactRange: 1,
            estimateStep: F("0.25"),
            contactMemoryDays: 30);

        public static NarrativeCatalog CreateNarrative()
        {
            var events = new[]
            {
                new EventDefinition(BaselineNarrative.GreatDepression, "olay.buyukBuhran", CountryId.None,
                    new[] { new Condition(ConditionKind.DateOnOrAfter, (ulong)GameDate.FromYmd(1929, 10, 24).DayNumber, Fixed64.Zero) },
                    meanDays: 30, firesOnce: true, responseDays: 30,
                    new[]
                    {
                        new EventOption("secenek.kemerSikma", new[] { Pct(EconomyStatKeys.ProductionEfficiency, "-0.10") }, 365, F("2000")),
                        new EventOption("secenek.kamuHarcamasi", new[] { Pct(EconomyStatKeys.ProductionEfficiency, "-0.05"), Add(PoliticsStatKeys.StabilityTarget, "0.03") }, 365, F("-5000")),
                    }),
                new EventDefinition(BaselineNarrative.WarWeariness, "olay.savasYorgunlugu", CountryId.None,
                    new[] { new Condition(ConditionKind.AtWar, 0, Fixed64.Zero) },
                    meanDays: 180, firesOnce: false, responseDays: 14,
                    new[] { new EventOption("secenek.katlan", new[] { Add(PoliticsStatKeys.StabilityTarget, "-0.05") }, 90, Fixed64.Zero) }),
                new EventDefinition(BaselineNarrative.IndustrialBoom, "olay.sanayiAtilimi", CountryId.None,
                    new[] { new Condition(ConditionKind.FocusCompleted, BaselineNarrative.Industrialization.Value, Fixed64.Zero) },
                    meanDays: 20, firesOnce: true, responseDays: 30,
                    new[] { new EventOption("secenek.yatirim", new[] { Pct(EconomyStatKeys.ProductionEfficiency, "0.05") }, 180, F("-1000")) }),
            };

            var focuses = new[]
            {
                new FocusDefinition(BaselineNarrative.Industrialization, "odak.sanayilesme", CountryId.None, 60, null, null, null,
                    new[] { Pct(EconomyStatKeys.ProductionEfficiency, "0.05") }, Fixed64.Zero),
                new FocusDefinition(BaselineNarrative.HeavyIndustry, "odak.agirSanayi", CountryId.None, 90, new[] { BaselineNarrative.Industrialization }, null, null,
                    new[] { Pct(EconomyStatKeys.ConstructionSpeed, "0.10") }, Fixed64.Zero),
                new FocusDefinition(BaselineNarrative.ArmyModernization, "odak.ordununModernlesmesi", CountryId.None, 70, null, new[] { BaselineNarrative.NavalBuildup }, null,
                    new[] { Pct(WarfareStatKeys.LandAttack, "0.05") }, Fixed64.Zero),
                new FocusDefinition(BaselineNarrative.NavalBuildup, "odak.donanmaInsasi", CountryId.None, 70, null, new[] { BaselineNarrative.ArmyModernization }, null,
                    new[] { Pct(WarfareStatKeys.NavalAttack, "0.10") }, Fixed64.Zero),
                new FocusDefinition(BaselineNarrative.Neutrality, "odak.tarafsizlik", CountryId.None, 50, null, null,
                    new[] { new Condition(ConditionKind.NotAtWar, 0, Fixed64.Zero) },
                    new[] { Add(PoliticsStatKeys.StabilityTarget, "0.05") }, F("5000")),
            };

            Result<NarrativeCatalog> catalog = NarrativeCatalog.Create(events, focuses);
            if (!catalog.IsSuccess)
            {
                throw new InvalidOperationException("Temel olay/odak içeriği geçersiz: " + catalog.ErrorKey);
            }

            return catalog.Value;
        }

        private static EffectGrant Pct(string stat, string value) => new EffectGrant(stat, EffectOperation.Percent, F(value));

        private static EffectGrant Add(string stat, string value) => new EffectGrant(stat, EffectOperation.Add, F(value));

        private static Fixed64 F(string text) => Fixed64.Parse(text);
    }
}
