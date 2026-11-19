using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Warfare.State;

namespace TarihStrateji.Core.Warfare
{
    /// <summary><see cref="WarfareState"/>'in kanonik anlık görüntüsü.</summary>
    public static class WarfareSnapshotCodec
    {
        public const uint Tag = 0x5241574C; // "LWAR"

        public static void Write(WarfareState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteUInt64(state.UnitIds.NextValue);
            w.WriteInt32(state.Units.Count);
            for (int i = 0; i < state.Units.Count; i++)
            {
                LandUnit u = state.Units.ValueAt(i);
                w.WriteId(u.Id);
                w.WriteId(u.Owner);
                w.WriteId(u.Type);
                w.WriteId(u.Hex);
                w.WriteId(u.HomeRegion);
                w.WriteInt64(u.Manpower);
                w.WriteFixed64(u.Equipment);
                w.WriteFixed64(u.Organization);
                w.WriteFixed64(u.ActionPoints);
                w.WriteFixed64(u.Supply);
                w.WriteFixed64(u.MoveBudget);
                w.WriteId(u.AttackTarget);
                w.WriteId(u.LastResolution);
                w.WriteInt32(u.Path.Count);
                for (int p = 0; p < u.Path.Count; p++)
                {
                    w.WriteId(u.Path[p]);
                }
            }

            w.WriteInt32(state.Besieged.Count);
            for (int i = 0; i < state.Besieged.Count; i++)
            {
                w.WriteId(state.Besieged[i]);
            }
        }

        public static Result<WarfareState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new WarfareState { UnitIds = new StableIdAllocator(r.ReadUInt64()) };
                int count = r.ReadCount(r.Remaining);
                for (int i = 0; i < count; i++)
                {
                    var u = new LandUnit(new LandUnitId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), new UnitTypeId(r.ReadUInt64()), new HexId(r.ReadUInt64()), new RegionId(r.ReadUInt64()))
                    {
                        Manpower = r.ReadInt64(),
                        Equipment = r.ReadFixed64(),
                        Organization = r.ReadFixed64(),
                        ActionPoints = r.ReadFixed64(),
                        Supply = r.ReadFixed64(),
                        MoveBudget = r.ReadFixed64(),
                        AttackTarget = new HexId(r.ReadUInt64()),
                        LastResolution = new ResolutionId(r.ReadUInt64()),
                    };
                    int path = r.ReadCount(r.Remaining);
                    for (int p = 0; p < path; p++)
                    {
                        u.PathList.Add(new HexId(r.ReadUInt64()));
                    }

                    if (!u.Id.IsValid || u.Id.Value >= state.UnitIds.NextValue)
                    {
                        return Result<WarfareState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    state.Units.Add(u.Id, u);
                }

                int besieged = r.ReadCount(r.Remaining);
                for (int i = 0; i < besieged; i++)
                {
                    state.BesiegedList.Add(new CityId(r.ReadUInt64()));
                }

                return Result<WarfareState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<WarfareState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
