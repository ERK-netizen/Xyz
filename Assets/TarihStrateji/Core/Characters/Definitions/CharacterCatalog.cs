using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Effects;

namespace TarihStrateji.Core.Characters.Definitions
{
    /// <summary>Doğrulanmış karakter tanımları.</summary>
    public sealed class CharacterCatalog
    {
        private CharacterCatalog(StableIdMap<CharacterId, CharacterDefinition> characters)
        {
            Characters = characters;
        }

        public StableIdMap<CharacterId, CharacterDefinition> Characters { get; }

        public static Result<CharacterCatalog> Create(IReadOnlyList<CharacterDefinition> characters, int eraCount)
        {
            if (characters == null)
            {
                throw new ArgumentNullException(nameof(characters));
            }

            var map = new StableIdMap<CharacterId, CharacterDefinition>(characters.Count);
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterDefinition c = characters[i];
                if (c == null || !c.Id.IsValid || !c.Country.IsValid || string.IsNullOrEmpty(c.Key) || c.Roles == CharacterRole.None
                    || c.ServiceUntil < c.ServiceFrom || c.MinimumEra < 0 || c.MinimumEra >= eraCount
                    || !EffectGrants.AllValid(c.Traits) || !map.TryAdd(c.Id, c))
                {
                    return Result<CharacterCatalog>.Failure(CharacterErrorKeys.TanimGecersiz);
                }
            }

            return Result<CharacterCatalog>.Success(new CharacterCatalog(map));
        }
    }
}
