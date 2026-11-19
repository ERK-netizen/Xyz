using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Diplomacy.Definitions
{
    /// <summary>
    /// Diplomasi ayarları (veri). Süreler gün cinsindendir.
    /// <list type="bullet">
    /// <item><see cref="ProposalLifetimeDays"/>: yanıtsız teklif bu sürede düşer.</item>
    /// <item><see cref="NonAggressionDays"/>, <see cref="AllianceDays"/>: antlaşma süreleri.</item>
    /// <item><see cref="TruceDays"/>: barıştan sonra aynı ülkeye savaş açılamayan süre.</item>
    /// <item><see cref="DemandControlShare"/>: barışta bir bölge talep etmek için o bölgenin bu
    /// oranından fazla hücresini fiilen kontrol etmek gerekir.</item>
    /// <item><see cref="CapitulationThreshold"/>: savaştaki ülkenin sahip olduğu şehirlerden
    /// kontrolünde kalanların oranı bunun altına inerse teslim olur (müttefik savaşıyorsa sürgün).</item>
    /// </list>
    /// </summary>
    public sealed class DiplomacyParameters
    {
        public DiplomacyParameters(int proposalLifetimeDays, int nonAggressionDays, int allianceDays, int truceDays, Fixed64 demandControlShare, Fixed64 capitulationThreshold)
        {
            if (proposalLifetimeDays <= 0 || nonAggressionDays <= 0 || allianceDays <= 0 || truceDays < 0
                || demandControlShare.IsNegative || demandControlShare >= Fixed64.One || capitulationThreshold.IsNegative || capitulationThreshold > Fixed64.One)
            {
                throw new ArgumentException(DiplomacyErrorKeys.TanimGecersiz);
            }

            ProposalLifetimeDays = proposalLifetimeDays;
            NonAggressionDays = nonAggressionDays;
            AllianceDays = allianceDays;
            TruceDays = truceDays;
            DemandControlShare = demandControlShare;
            CapitulationThreshold = capitulationThreshold;
        }

        public int ProposalLifetimeDays { get; }

        public int NonAggressionDays { get; }

        public int AllianceDays { get; }

        public int TruceDays { get; }

        public Fixed64 DemandControlShare { get; }

        public Fixed64 CapitulationThreshold { get; }
    }
}
