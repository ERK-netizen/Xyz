using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Effects.Events
{
    /// <summary><see cref="NarrativeState"/>'in kanonik anlık görüntüsü.</summary>
    public static class NarrativeSnapshotCodec
    {
        public const uint Tag = 0x52524E4E; // "NNRR"

        public static void Write(NarrativeState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Countries.Count);
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryNarrative n = state.Countries.ValueAt(i);
                w.WriteId(n.Country);
                w.WriteInt32(n.Fired.Count);
                for (int f = 0; f < n.Fired.Count; f++)
                {
                    w.WriteId(n.Fired.KeyAt(f));
                    w.WriteDate(n.Fired.ValueAt(f));
                }

                w.WriteInt32(n.Pending.Count);
                for (int p = 0; p < n.Pending.Count; p++)
                {
                    w.WriteId(n.Pending[p].EventId);
                    w.WriteDate(n.Pending[p].FiredOn);
                    w.WriteDate(n.Pending[p].LastDay);
                }

                w.WriteId(n.CurrentFocus);
                w.WriteInt32(n.FocusProgressDays);
                w.WriteInt32(n.CompletedFocuses.Count);
                for (int f = 0; f < n.CompletedFocuses.Count; f++)
                {
                    w.WriteId(n.CompletedFocuses.KeyAt(f));
                    w.WriteDate(n.CompletedFocuses.ValueAt(f));
                }
            }
        }

        public static Result<NarrativeState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new NarrativeState();
                int countries = r.ReadCount(r.Remaining);
                for (int i = 0; i < countries; i++)
                {
                    var country = new CountryId(r.ReadUInt64());
                    if (!country.IsValid || state.Countries.ContainsKey(country))
                    {
                        return Result<NarrativeState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    CountryNarrative n = state.For(country);
                    int fired = r.ReadCount(r.Remaining);
                    for (int f = 0; f < fired; f++)
                    {
                        if (!n.Fired.TryAdd(new EventId(r.ReadUInt64()), r.ReadDate()))
                        {
                            return Result<NarrativeState>.Failure(DomainErrorKeys.KayitBozuk);
                        }
                    }

                    int pending = r.ReadCount(r.Remaining);
                    for (int p = 0; p < pending; p++)
                    {
                        n.PendingList.Add(new PendingEvent(new EventId(r.ReadUInt64()), r.ReadDate(), r.ReadDate()));
                    }

                    n.CurrentFocus = new FocusId(r.ReadUInt64());
                    n.FocusProgressDays = r.ReadInt32();
                    int completed = r.ReadCount(r.Remaining);
                    for (int f = 0; f < completed; f++)
                    {
                        if (!n.CompletedFocuses.TryAdd(new FocusId(r.ReadUInt64()), r.ReadDate()))
                        {
                            return Result<NarrativeState>.Failure(DomainErrorKeys.KayitBozuk);
                        }
                    }

                    if (n.FocusProgressDays < 0)
                    {
                        return Result<NarrativeState>.Failure(DomainErrorKeys.KayitBozuk);
                    }
                }

                return Result<NarrativeState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<NarrativeState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
