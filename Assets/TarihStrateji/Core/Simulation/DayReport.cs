using System.Collections.Generic;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>Bir günlük adımın raporu (arayüz, günlük kayıt ve testler için).</summary>
    public sealed class DayReport
    {
        internal DayReport(
            GameDate date,
            int appliedCommands,
            List<string> rejectedCommands,
            int expiredEffects,
            List<string> notes,
            List<string> violations,
            byte[] stateHash,
            bool autosaved)
        {
            Date = date;
            AppliedCommands = appliedCommands;
            RejectedCommands = rejectedCommands;
            ExpiredEffects = expiredEffects;
            Notes = notes;
            Violations = violations;
            StateHash = stateHash;
            Autosaved = autosaved;
        }

        public GameDate Date { get; }

        public int AppliedCommands { get; }

        /// <summary>P1'de artık geçerli olmadığı için uygulanmayan emirler: "CommandId: anahtar".</summary>
        public IReadOnlyList<string> RejectedCommands { get; }

        public int ExpiredEffects { get; }

        public IReadOnlyList<string> Notes { get; }

        /// <summary>Değişmez ihlalleri veya sistem hatası; boş değilse simülasyon durmuştur.</summary>
        public IReadOnlyList<string> Violations { get; }

        public bool Halted => Violations.Count > 0;

        /// <summary>Bu gün hash hesaplandıysa SHA-256 (32 bayt), değilse <c>null</c>.</summary>
        public byte[] StateHash { get; }

        public bool Autosaved { get; }
    }
}
