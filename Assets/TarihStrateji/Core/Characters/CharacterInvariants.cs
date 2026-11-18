using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Characters
{
    /// <summary>Atama değişmezleri: karakter ülkenin, rolü uygun, hizmette ve tek pozisyonda.</summary>
    public static class CharacterInvariants
    {
        public static int Check(CharacterState state, CharacterCatalog catalog, GameDate today, List<string> violations)
        {
            int before = violations.Count;
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryCharacters cc = state.Countries.ValueAt(i);
                string at = " @ " + cc.Country.Value.ToString(CultureInfo.InvariantCulture);
                for (int p = 0; p < Positions.Count; p++)
                {
                    CharacterId holder = cc.Holders[p];
                    if (!holder.IsValid)
                    {
                        if (cc.AppliedEffects[p].Count > 0)
                        {
                            violations.Add(CharacterErrorKeys.DurumTutarsiz + at);
                        }

                        continue;
                    }

                    if (!catalog.Characters.TryGetValue(holder, out CharacterDefinition def) || def.Country != cc.Country
                        || !def.Has(Positions.RequiredRole((PositionKind)p)) || !def.InService(today))
                    {
                        violations.Add(CharacterErrorKeys.DurumTutarsiz + at);
                        continue;
                    }

                    for (int q = p + 1; q < Positions.Count; q++)
                    {
                        if (cc.Holders[q] == holder)
                        {
                            violations.Add(CharacterErrorKeys.ZatenGorevde + at);
                        }
                    }
                }
            }

            return violations.Count - before;
        }
    }
}
