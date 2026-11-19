using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>Ülke başına <see cref="ObservedWorld"/>.</summary>
    public sealed class IntelligenceState
    {
        public StableIdMap<CountryId, ObservedWorld> Countries { get; } = new StableIdMap<CountryId, ObservedWorld>();

        /// <summary>Ülkenin görüşünü döndürür; yoksa oluşturur.</summary>
        public ObservedWorld For(CountryId country)
        {
            if (!Countries.TryGetValue(country, out ObservedWorld view))
            {
                view = new ObservedWorld(country);
                Countries.Add(country, view);
            }

            return view;
        }
    }
}
