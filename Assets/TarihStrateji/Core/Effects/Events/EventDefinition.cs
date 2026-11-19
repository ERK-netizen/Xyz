using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>
    /// Olay: koşulları sağlayan ülkede her gün <c>1 / MeanDays</c> olasılıkla tetiklenir
    /// (deterministik PRNG, <c>eventId</c> = olay kimliği). Ülke <see cref="ResponseDays"/> içinde
    /// seçenek seçer; seçmezse ilk seçenek uygulanır. <see cref="Country"/> geçerliyse yalnız o ülke.
    /// </summary>
    public sealed class EventDefinition
    {
        private readonly Condition[] _conditions;
        private readonly EventOption[] _options;

        public EventDefinition(EventId id, string key, CountryId country, Condition[] conditions, int meanDays, bool firesOnce, int responseDays, EventOption[] options)
        {
            Id = id;
            Key = key;
            Country = country;
            _conditions = conditions ?? new Condition[0];
            MeanDays = meanDays;
            FiresOnce = firesOnce;
            ResponseDays = responseDays;
            _options = options ?? new EventOption[0];
        }

        public EventId Id { get; }

        public string Key { get; }

        public CountryId Country { get; }

        public IReadOnlyList<Condition> Conditions => _conditions;

        public int MeanDays { get; }

        public bool FiresOnce { get; }

        public int ResponseDays { get; }

        public IReadOnlyList<EventOption> Options => _options;
    }
}
