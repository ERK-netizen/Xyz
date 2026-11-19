using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Yanıt bekleyen olay; <see cref="LastDay"/>'den sonra ilk seçenek uygulanır.</summary>
    public sealed class PendingEvent
    {
        internal PendingEvent(EventId eventId, GameDate firedOn, GameDate lastDay)
        {
            EventId = eventId;
            FiredOn = firedOn;
            LastDay = lastDay;
        }

        public EventId EventId { get; }

        public GameDate FiredOn { get; }

        public GameDate LastDay { get; }
    }
}
