using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary>Ülke başına olay/odak durumu.</summary>
    public sealed class NarrativeState
    {
        public StableIdMap<CountryId, CountryNarrative> Countries { get; } = new StableIdMap<CountryId, CountryNarrative>();

        public CountryNarrative For(CountryId country)
        {
            if (!Countries.TryGetValue(country, out CountryNarrative n))
            {
                n = new CountryNarrative(country);
                Countries.Add(country, n);
            }

            return n;
        }
    }
}
