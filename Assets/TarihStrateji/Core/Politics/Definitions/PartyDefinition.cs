using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Politics.Definitions
{
    /// <summary>Siyasi parti (içerik verisi); bir ülkeye aittir.</summary>
    public sealed class PartyDefinition
    {
        public PartyDefinition(PartyId id, string key, CountryId country, Ideology ideology)
        {
            Id = id;
            Key = key;
            Country = country;
            Ideology = ideology;
        }

        public PartyId Id { get; }

        public string Key { get; }

        public CountryId Country { get; }

        public Ideology Ideology { get; }
    }
}
