using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Population
{
    /// <summary><see cref="PopulationState"/>'in kanonik anlık görüntüsü (carry dahil: yeniden oynatma için gerekir).</summary>
    public static class PopulationSnapshotCodec
    {
        public const uint Tag = 0x504F504E; // "NPOP"

        public static void Write(PopulationState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Regions.Count);
            for (int i = 0; i < state.Regions.Count; i++)
            {
                RegionPopulation p = state.Regions.ValueAt(i);
                w.WriteId(p.Region);
                w.WriteInt64(p.Total);
                w.WriteInt64(p.Mobilized);
                w.WriteInt64(p.Employed);
                w.WriteFixed64(p.BirthCarry);
                for (int b = 0; b < AgeBands.Count; b++)
                {
                    w.WriteInt64(p.Cohorts[b]);
                    w.WriteFixed64(p.DeathCarry[b]);
                    w.WriteFixed64(p.AgingCarry[b]);
                }
            }
        }

        public static Result<PopulationState> Read(CanonicalReader r)
        {
            r.ExpectTag(Tag);
            var state = new PopulationState();
            int count = r.ReadCount(r.Remaining);
            for (int i = 0; i < count; i++)
            {
                var region = new RegionId(r.ReadUInt64());
                var p = new RegionPopulation(region)
                {
                    Total = r.ReadInt64(),
                    Mobilized = r.ReadInt64(),
                    Employed = r.ReadInt64(),
                    BirthCarry = r.ReadFixed64(),
                };
                for (int b = 0; b < AgeBands.Count; b++)
                {
                    p.Cohorts[b] = r.ReadInt64();
                    p.DeathCarry[b] = r.ReadFixed64();
                    p.AgingCarry[b] = r.ReadFixed64();
                }

                if (!region.IsValid || p.Total != p.SumCohorts() || p.Mobilized < 0L || p.Employed < 0L)
                {
                    return Result<PopulationState>.Failure(DomainErrorKeys.KayitBozuk);
                }

                try
                {
                    state.Regions.Add(region, p);
                }
                catch (ArgumentException)
                {
                    return Result<PopulationState>.Failure(DomainErrorKeys.KayitBozuk);
                }
            }

            return Result<PopulationState>.Success(state);
        }
    }
}
