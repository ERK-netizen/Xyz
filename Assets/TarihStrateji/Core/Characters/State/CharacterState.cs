using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Characters.State
{
    /// <summary>Tüm ülkelerin atamaları.</summary>
    public sealed class CharacterState
    {
        public StableIdMap<CountryId, CountryCharacters> Countries { get; } = new StableIdMap<CountryId, CountryCharacters>();

        public Result<CountryCharacters> AddCountry(CountryId country)
        {
            if (!country.IsValid || Countries.ContainsKey(country))
            {
                return Result<CountryCharacters>.Failure(CharacterErrorKeys.UlkeZatenVar);
            }

            var c = new CountryCharacters(country);
            Countries.Add(country, c);
            return Result<CountryCharacters>.Success(c);
        }
    }
}
