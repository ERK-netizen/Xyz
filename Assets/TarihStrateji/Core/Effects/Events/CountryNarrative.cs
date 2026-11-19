using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Bir ülkenin olay geçmişi, bekleyen olayları ve odak ilerlemesi.</summary>
    public sealed class CountryNarrative
    {
        internal CountryNarrative(CountryId country)
        {
            Country = country;
        }

        public CountryId Country { get; }

        /// <summary>Tetiklenmiş olaylar ve tetiklendikleri gün (bir kez olanlar için).</summary>
        public StableIdMap<EventId, GameDate> Fired { get; } = new StableIdMap<EventId, GameDate>();

        internal List<PendingEvent> PendingList { get; } = new List<PendingEvent>();

        /// <summary>Bekleyen olaylar (olay kimliği sırasıyla).</summary>
        public IReadOnlyList<PendingEvent> Pending => PendingList;

        public FocusId CurrentFocus { get; internal set; }

        public int FocusProgressDays { get; internal set; }

        public StableIdMap<FocusId, GameDate> CompletedFocuses { get; } = new StableIdMap<FocusId, GameDate>();

        public int IndexOfPending(EventId id)
        {
            for (int i = 0; i < PendingList.Count; i++)
            {
                if (PendingList[i].EventId == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
