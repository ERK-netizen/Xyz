using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Research.State;

namespace TarihStrateji.Core.Research
{
    /// <summary><see cref="ResearchState"/>'in kanonik anlık görüntüsü.</summary>
    public static class ResearchSnapshotCodec
    {
        public const uint Tag = 0x48435352; // "RSCH"

        public static void Write(ResearchState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Countries.Count);
            for (int i = 0; i < state.Countries.Count; i++)
            {
                CountryResearch r = state.Countries.ValueAt(i);
                w.WriteId(r.Country);
                w.WriteInt32(r.Era);
                w.WriteId(r.Current);
                w.WriteInt32(r.Researched.Count);
                for (int t = 0; t < r.Researched.Count; t++)
                {
                    w.WriteId(r.Researched.KeyAt(t));
                    w.WriteDate(r.Researched.ValueAt(t));
                }

                w.WriteInt32(r.Progress.Count);
                for (int p = 0; p < r.Progress.Count; p++)
                {
                    w.WriteId(r.Progress.KeyAt(p));
                    w.WriteFixed64(r.Progress.ValueAt(p));
                }

                w.WriteInt32(r.Queue.Count);
                for (int q = 0; q < r.Queue.Count; q++)
                {
                    w.WriteId(r.Queue[q]);
                }
            }
        }

        public static Result<ResearchState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new ResearchState();
                int count = r.ReadCount(r.Remaining);
                for (int i = 0; i < count; i++)
                {
                    var country = new CountryId(r.ReadUInt64());
                    var research = new CountryResearch(country, r.ReadInt32()) { Current = new TechnologyId(r.ReadUInt64()) };
                    int researched = r.ReadCount(r.Remaining);
                    for (int t = 0; t < researched; t++)
                    {
                        research.Researched.Add(new TechnologyId(r.ReadUInt64()), r.ReadDate());
                    }

                    int progress = r.ReadCount(r.Remaining);
                    for (int p = 0; p < progress; p++)
                    {
                        research.Progress.Add(new TechnologyId(r.ReadUInt64()), r.ReadFixed64());
                    }

                    int queue = r.ReadCount(ResearchRules.MaxQueue);
                    for (int q = 0; q < queue; q++)
                    {
                        research.QueueList.Add(new TechnologyId(r.ReadUInt64()));
                    }

                    state.Countries.Add(country, research);
                }

                return Result<ResearchState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<ResearchState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
