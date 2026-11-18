using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>Emir günlüğü kaydı. Anında çözülen emirler <see cref="Resolution"/> taşır.</summary>
    public sealed class JournalEntry
    {
        internal JournalEntry(CommandId id, GameDate submittedOn, GameCommand command, ResolutionId resolution)
        {
            Id = id;
            SubmittedOn = submittedOn;
            Command = command;
            Resolution = resolution;
        }

        public CommandId Id { get; }

        public GameDate SubmittedOn { get; }

        public GameCommand Command { get; }

        /// <summary>Anında çözüldüyse çözüm kimliği; günlük P1'de uygulanacaksa <see cref="ResolutionId.None"/>.</summary>
        public ResolutionId Resolution { get; }

        public bool IsResolved => Resolution.IsValid;
    }
}
