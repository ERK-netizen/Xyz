using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>Yönetim biçimi (içerik verisi).</summary>
    public sealed class GovernmentFormDefinition
    {
        public GovernmentFormDefinition(GovernmentFormId id, string key, int electionIntervalDays, Fixed64 baseStability)
        {
            Id = id;
            Key = key;
            ElectionIntervalDays = electionIntervalDays;
            BaseStability = baseStability;
        }

        public GovernmentFormId Id { get; }

        public string Key { get; }

        /// <summary>Seçim aralığı (gün); 0 = seçim yok.</summary>
        public int ElectionIntervalDays { get; }

        public bool HoldsElections => ElectionIntervalDays > 0;

        public Fixed64 BaseStability { get; }
    }
}
