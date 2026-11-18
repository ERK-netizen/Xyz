using System;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Characters
{
    /// <summary><see cref="CharacterState"/>'in kanonik anlık görüntüsü.</summary>
    public static class CharacterSnapshotCodec
    {
        public const uint Tag = 0x52524843; // "CHRR"

        public static void Write(CharacterState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Countries.Count);
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryCharacters cc = state.Countries.ValueAt(i);
                w.WriteId(cc.Country);
                for (int p = 0; p < Positions.Count; p++)
                {
                    w.WriteId(cc.Holders[p]);
                    w.WriteInt32(cc.AppliedEffects[p].Count);
                    for (int e = 0; e < cc.AppliedEffects[p].Count; e++)
                    {
                        w.WriteId(cc.AppliedEffects[p][e]);
                    }
                }
            }
        }

        public static Result<CharacterState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new CharacterState();
                int count = r.ReadCount(r.Remaining);
                for (int i = 0; i < count; i++)
                {
                    CountryCharacters cc = state.AddCountry(new CountryId(r.ReadUInt64())).Value;
                    for (int p = 0; p < Positions.Count; p++)
                    {
                        cc.Holders[p] = new CharacterId(r.ReadUInt64());
                        int effects = r.ReadCount(r.Remaining);
                        for (int e = 0; e < effects; e++)
                        {
                            cc.AppliedEffects[p].Add(new EffectId(r.ReadUInt64()));
                        }
                    }
                }

                return Result<CharacterState>.Success(state);
            }
            catch (InvalidOperationException)
            {
                return Result<CharacterState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
