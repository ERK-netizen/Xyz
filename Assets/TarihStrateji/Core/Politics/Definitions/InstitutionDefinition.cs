using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>
    /// Kurum (ordu, meclis, yargı…; içerik verisi). Gücü [0, 1]; tam güçte istikrar hedefine
    /// <see cref="StabilityContribution"/> katar. Yatırımla güçlenir, her gün aşınır.
    /// </summary>
    public sealed class InstitutionDefinition
    {
        public InstitutionDefinition(InstitutionId id, string key, Fixed64 stabilityContribution, Fixed64 moneyPerStrength)
        {
            Id = id;
            Key = key;
            StabilityContribution = stabilityContribution;
            MoneyPerStrength = moneyPerStrength;
        }

        public InstitutionId Id { get; }

        public string Key { get; }

        public Fixed64 StabilityContribution { get; }

        /// <summary>Gücü 1 artırmanın para bedeli.</summary>
        public Fixed64 MoneyPerStrength { get; }
    }
}
