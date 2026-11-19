using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Uygulanacak sonuç: bir olay seçeneği (<see cref="Focus"/> geçersiz) veya tamamlanan odak.
    /// Etkileri ve hazine değişimini Simulation uygular (hazine Economy'dedir).
    /// </summary>
    public readonly struct NarrativeOutcome
    {
        public NarrativeOutcome(CountryId country, EventId eventId, int option, FocusId focus)
        {
            Country = country;
            EventId = eventId;
            Option = option;
            Focus = focus;
        }

        public CountryId Country { get; }

        public EventId EventId { get; }

        public int Option { get; }

        public FocusId Focus { get; }

        public bool IsFocus => Focus.IsValid;
    }
}
