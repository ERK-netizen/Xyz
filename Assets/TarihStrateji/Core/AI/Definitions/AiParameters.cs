using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.AI.Definitions
{
    /// <summary>
    /// Yapay zekâ ayarları (tanım verisi; <c>Core.Content</c>'te kurulur). Güç birimi "tümen
    /// eşdeğeri"dir: bir birliğin gücü (0–1) toplamı.
    /// <list type="bullet">
    /// <item><see cref="DecisionIntervalDays"/>: stratejik/diplomatik/yönetim kararlarının aralığı
    /// (ülkeler kimliğe göre kaydırılır); ordu katmanı her gün çalışır.</item>
    /// <item><see cref="Aggression"/>: 0 ise YZ asla savaş ilan etmez.</item>
    /// <item><see cref="DeclareWarPowerRatio"/>: kendi gücü / hedefin tahmini gücü en az bu olmalı.</item>
    /// <item><see cref="GarrisonEstimatePerCity"/>: görülmeyen düşman için şehir başına varsayılan güç.</item>
    /// <item><see cref="AttackPowerRatio"/>, <see cref="AttackMinOrganization"/>: saldırı eşikleri.</item>
    /// <item><see cref="PeaceAfterWarDays"/>, <see cref="StalemateDays"/>, <see cref="LosingHeldShare"/>,
    /// <see cref="LosingPowerRatio"/>: barış önerme/kabul eşikleri.</item>
    /// <item><see cref="MaxMobilizationShare"/>: toplama, seferber nüfus bu oranı aşmayacak kadar yapılır.</item>
    /// <item><see cref="ConstructionReserve"/>, <see cref="MaxConcurrentConstructions"/>, <see cref="BuildPriority"/>:
    /// hazine yedeğin üstündeyse tercih sırasındaki ilk uygun tesise yatırım.</item>
    /// <item><see cref="TreasuryHigh"/>/<see cref="TreasuryLow"/>: maliye dengeleyicisi; vergi
    /// <see cref="TaxStep"/> adımlarla [<see cref="MinTaxRate"/>, <see cref="MaxTaxRate"/>] içinde.</item>
    /// </list>
    /// </summary>
    public sealed class AiParameters
    {
        public AiParameters(
            int decisionIntervalDays,
            Fixed64 aggression,
            Fixed64 declareWarPowerRatio,
            Fixed64 minimumWarPower,
            Fixed64 declareWarMinStability,
            Fixed64 garrisonEstimatePerCity,
            Fixed64 attackPowerRatio,
            Fixed64 attackMinOrganization,
            int defendRadius,
            int advanceSearchRadius,
            int unitsPerAdvanceTarget,
            int peaceAfterWarDays,
            int stalemateDays,
            Fixed64 losingHeldShare,
            Fixed64 losingPowerRatio,
            Fixed64 recruitPerCity,
            Fixed64 recruitThreatMargin,
            UnitTypeId recruitUnitType,
            Fixed64 maxMobilizationShare,
            Fixed64 constructionReserve,
            int maxConcurrentConstructions,
            FacilityTypeId[] buildPriority,
            Fixed64 treasuryHigh,
            Fixed64 treasuryLow,
            Fixed64 taxStep,
            Fixed64 minTaxRate,
            Fixed64 maxTaxRate)
        {
            Fixed64[] fractions = { aggression, declareWarMinStability, attackMinOrganization, losingHeldShare, losingPowerRatio, maxMobilizationShare, taxStep, minTaxRate, maxTaxRate };
            for (int i = 0; i < fractions.Length; i++)
            {
                if (fractions[i].IsNegative || fractions[i] > Fixed64.One)
                {
                    throw new ArgumentException(AiErrorKeys.TanimGecersiz);
                }
            }

            if (decisionIntervalDays < 1 || !declareWarPowerRatio.IsPositive || minimumWarPower.IsNegative || garrisonEstimatePerCity.IsNegative
                || !attackPowerRatio.IsPositive || defendRadius < 0 || advanceSearchRadius < 1 || unitsPerAdvanceTarget < 1
                || peaceAfterWarDays < 0 || stalemateDays < peaceAfterWarDays || recruitPerCity.IsNegative || recruitThreatMargin.IsNegative
                || !recruitUnitType.IsValid || constructionReserve.IsNegative || maxConcurrentConstructions < 0 || buildPriority == null
                || treasuryLow.IsNegative || treasuryHigh <= treasuryLow || minTaxRate > maxTaxRate)
            {
                throw new ArgumentException(AiErrorKeys.TanimGecersiz);
            }

            DecisionIntervalDays = decisionIntervalDays;
            Aggression = aggression;
            DeclareWarPowerRatio = declareWarPowerRatio;
            MinimumWarPower = minimumWarPower;
            DeclareWarMinStability = declareWarMinStability;
            GarrisonEstimatePerCity = garrisonEstimatePerCity;
            AttackPowerRatio = attackPowerRatio;
            AttackMinOrganization = attackMinOrganization;
            DefendRadius = defendRadius;
            AdvanceSearchRadius = advanceSearchRadius;
            UnitsPerAdvanceTarget = unitsPerAdvanceTarget;
            PeaceAfterWarDays = peaceAfterWarDays;
            StalemateDays = stalemateDays;
            LosingHeldShare = losingHeldShare;
            LosingPowerRatio = losingPowerRatio;
            RecruitPerCity = recruitPerCity;
            RecruitThreatMargin = recruitThreatMargin;
            RecruitUnitType = recruitUnitType;
            MaxMobilizationShare = maxMobilizationShare;
            ConstructionReserve = constructionReserve;
            MaxConcurrentConstructions = maxConcurrentConstructions;
            _buildPriority = (FacilityTypeId[])buildPriority.Clone();
            TreasuryHigh = treasuryHigh;
            TreasuryLow = treasuryLow;
            TaxStep = taxStep;
            MinTaxRate = minTaxRate;
            MaxTaxRate = maxTaxRate;
        }

        private readonly FacilityTypeId[] _buildPriority;

        public int DecisionIntervalDays { get; }

        public Fixed64 Aggression { get; }

        public Fixed64 DeclareWarPowerRatio { get; }

        public Fixed64 MinimumWarPower { get; }

        public Fixed64 DeclareWarMinStability { get; }

        public Fixed64 GarrisonEstimatePerCity { get; }

        public Fixed64 AttackPowerRatio { get; }

        public Fixed64 AttackMinOrganization { get; }

        public int DefendRadius { get; }

        public int AdvanceSearchRadius { get; }

        public int UnitsPerAdvanceTarget { get; }

        public int PeaceAfterWarDays { get; }

        public int StalemateDays { get; }

        public Fixed64 LosingHeldShare { get; }

        public Fixed64 LosingPowerRatio { get; }

        public Fixed64 RecruitPerCity { get; }

        public Fixed64 RecruitThreatMargin { get; }

        public UnitTypeId RecruitUnitType { get; }

        /// <summary>Seferberlik tavanı: seferber / çalışma çağı (game-balance bandı %8–20).</summary>
        public Fixed64 MaxMobilizationShare { get; }

        /// <summary>İnşaata girişmeden hazinede bırakılan en az para.</summary>
        public Fixed64 ConstructionReserve { get; }

        public int MaxConcurrentConstructions { get; }

        /// <summary>İnşaat tercih sırası (ilk uygun tür ve sektör seçilir).</summary>
        public IReadOnlyList<FacilityTypeId> BuildPriority => _buildPriority;

        /// <summary>Maliye: hazine bunun üstündeyse vergi bir adım düşer.</summary>
        public Fixed64 TreasuryHigh { get; }

        /// <summary>Maliye: hazine bunun altındaysa vergi bir adım artar.</summary>
        public Fixed64 TreasuryLow { get; }

        public Fixed64 TaxStep { get; }

        public Fixed64 MinTaxRate { get; }

        public Fixed64 MaxTaxRate { get; }
    }
}
