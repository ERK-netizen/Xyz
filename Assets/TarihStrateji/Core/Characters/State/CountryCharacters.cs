using System.Collections.Generic;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Characters.State
{
    /// <summary>Ülkenin pozisyon atamaları ve her atamanın uyguladığı etki kimlikleri.</summary>
    public sealed class CountryCharacters
    {
        internal readonly CharacterId[] Holders = new CharacterId[Positions.Count];
        internal readonly List<EffectId>[] AppliedEffects = new List<EffectId>[Positions.Count];

        internal CountryCharacters(CountryId country)
        {
            Country = country;
            for (int i = 0; i < Positions.Count; i++)
            {
                AppliedEffects[i] = new List<EffectId>();
            }
        }

        public CountryId Country { get; }

        public CharacterId Holder(PositionKind position) => Holders[(int)position];

        public IReadOnlyList<EffectId> EffectsOf(PositionKind position) => AppliedEffects[(int)position];

        /// <summary>Karakterin tuttuğu pozisyon; yoksa false.</summary>
        public bool TryFindPosition(CharacterId character, out PositionKind position)
        {
            for (int i = 0; i < Positions.Count; i++)
            {
                if (Holders[i] == character && character.IsValid)
                {
                    position = (PositionKind)i;
                    return true;
                }
            }

            position = PositionKind.HeadOfGovernment;
            return false;
        }
    }
}
