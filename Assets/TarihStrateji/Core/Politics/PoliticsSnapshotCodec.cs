using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Politics.State;

namespace TarihStrateji.Core.Politics
{
    /// <summary><see cref="PoliticsState"/>'in kanonik anlık görüntüsü.</summary>
    public static class PoliticsSnapshotCodec
    {
        public const uint Tag = 0x544C4F50; // "POLT"

        public static void Write(PoliticsState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Countries.Count);
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryPolitics p = state.Countries.ValueAt(i);
                w.WriteId(p.Country);
                w.WriteId(p.Form);
                w.WriteId(p.RulingParty);
                w.WriteFixed64(p.Stability);
                w.WriteDate(p.NextElection);
                w.WriteInt32(p.PartySupport.Count);
                for (int k = 0; k < p.PartySupport.Count; k++)
                {
                    w.WriteId(p.PartySupport.KeyAt(k));
                    w.WriteFixed64(p.PartySupport.ValueAt(k));
                }

                w.WriteInt32(p.Institutions.Count);
                for (int k = 0; k < p.Institutions.Count; k++)
                {
                    w.WriteId(p.Institutions.KeyAt(k));
                    w.WriteFixed64(p.Institutions.ValueAt(k));
                }

                w.WriteInt32(p.ActiveCrises.Count);
                for (int k = 0; k < p.ActiveCrises.Count; k++)
                {
                    ActiveCrisis c = p.ActiveCrises.ValueAt(k);
                    w.WriteId(c.Crisis);
                    w.WriteDate(c.StartedOn);
                    w.WriteDate(c.LastDay);
                    w.WriteInt32(c.Effects.Count);
                    for (int e = 0; e < c.Effects.Count; e++)
                    {
                        w.WriteId(c.Effects[e]);
                    }
                }
            }
        }

        public static Result<PoliticsState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new PoliticsState();
                int count = r.ReadCount(r.Remaining);
                for (int i = 0; i < count; i++)
                {
                    var p = new CountryPolitics(new CountryId(r.ReadUInt64()))
                    {
                        Form = new GovernmentFormId(r.ReadUInt64()),
                        RulingParty = new PartyId(r.ReadUInt64()),
                        Stability = r.ReadFixed64(),
                        NextElection = r.ReadDate(),
                    };
                    int parties = r.ReadCount(r.Remaining);
                    for (int k = 0; k < parties; k++)
                    {
                        p.PartySupport.Add(new PartyId(r.ReadUInt64()), r.ReadFixed64());
                    }

                    int institutions = r.ReadCount(r.Remaining);
                    for (int k = 0; k < institutions; k++)
                    {
                        p.Institutions.Add(new InstitutionId(r.ReadUInt64()), r.ReadFixed64());
                    }

                    int crises = r.ReadCount(r.Remaining);
                    for (int k = 0; k < crises; k++)
                    {
                        var id = new CrisisId(r.ReadUInt64());
                        var started = r.ReadDate();
                        var last = r.ReadDate();
                        int effects = r.ReadCount(r.Remaining);
                        var list = new List<EffectId>(effects);
                        for (int e = 0; e < effects; e++)
                        {
                            list.Add(new EffectId(r.ReadUInt64()));
                        }

                        p.ActiveCrises.Add(id, new ActiveCrisis(id, started, last, list));
                    }

                    state.Countries.Add(p.Country, p);
                }

                return Result<PoliticsState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<PoliticsState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
