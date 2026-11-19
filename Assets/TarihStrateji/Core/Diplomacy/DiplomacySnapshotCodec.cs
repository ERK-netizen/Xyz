using System;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.Diplomacy
{
    /// <summary><see cref="DiplomacyState"/>'in kanonik anlık görüntüsü.</summary>
    public static class DiplomacySnapshotCodec
    {
        public const uint Tag = 0x4C504944; // "DIPL"

        public static void Write(DiplomacyState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteUInt64(state.WarIds.NextValue);
            w.WriteInt32(state.Wars.Count);
            for (int i = 0; i < state.Wars.Count; i++)
            {
                War war = state.Wars.ValueAt(i);
                w.WriteId(war.Id);
                w.WriteId(war.Attacker);
                w.WriteId(war.Defender);
                w.WriteDate(war.Start);
                w.WriteId(war.CalledBy);
                w.WriteInt32(war.Goals.Count);
                for (int g = 0; g < war.Goals.Count; g++)
                {
                    w.WriteId(war.Goals[g]);
                }
            }

            w.WriteUInt64(state.TreatyIds.NextValue);
            w.WriteInt32(state.Treaties.Count);
            for (int i = 0; i < state.Treaties.Count; i++)
            {
                Treaty t = state.Treaties.ValueAt(i);
                w.WriteId(t.Id);
                w.WriteByte((byte)t.Kind);
                w.WriteId(t.A);
                w.WriteId(t.B);
                w.WriteDate(t.Signed);
                w.WriteDate(t.LastDay);
            }

            w.WriteUInt64(state.ProposalIds.NextValue);
            w.WriteInt32(state.Proposals.Count);
            for (int i = 0; i < state.Proposals.Count; i++)
            {
                Proposal p = state.Proposals.ValueAt(i);
                w.WriteId(p.Id);
                w.WriteByte((byte)p.Kind);
                w.WriteId(p.From);
                w.WriteId(p.To);
                w.WriteDate(p.Created);
                w.WriteDate(p.LastDay);
                w.WriteByte((byte)p.Status);
                w.WriteInt32(p.Transfers.Count);
                for (int t = 0; t < p.Transfers.Count; t++)
                {
                    w.WriteId(p.Transfers[t].Region);
                    w.WriteId(p.Transfers[t].NewOwner);
                }
            }

            w.WriteInt32(state.Exiles.Count);
            for (int i = 0; i < state.Exiles.Count; i++)
            {
                w.WriteId(state.Exiles.KeyAt(i));
                w.WriteId(state.Exiles.ValueAt(i));
            }
        }

        public static Result<DiplomacyState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new DiplomacyState { WarIds = new StableIdAllocator(r.ReadUInt64()) };
                int wars = r.ReadCount(r.Remaining);
                for (int i = 0; i < wars; i++)
                {
                    var war = new War(new WarId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), r.ReadDate(), new WarId(r.ReadUInt64()));
                    int goals = r.ReadCount(r.Remaining);
                    for (int g = 0; g < goals; g++)
                    {
                        war.GoalList.Add(new RegionId(r.ReadUInt64()));
                    }

                    if (!war.Id.IsValid || war.Id.Value >= state.WarIds.NextValue || war.Attacker.Value > 0xFFFFFFFFUL || war.Defender.Value > 0xFFFFFFFFUL)
                    {
                        return Result<DiplomacyState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    state.Wars.Add(war.Id, war);
                }

                state.TreatyIds = new StableIdAllocator(r.ReadUInt64());
                int treaties = r.ReadCount(r.Remaining);
                for (int i = 0; i < treaties; i++)
                {
                    var id = new TreatyId(r.ReadUInt64());
                    byte kind = r.ReadByte();
                    var t = new Treaty(id, (TreatyKind)kind, new CountryId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), r.ReadDate(), r.ReadDate());
                    if (!id.IsValid || id.Value >= state.TreatyIds.NextValue || kind < (byte)TreatyKind.NonAggression || kind > (byte)TreatyKind.Peace)
                    {
                        return Result<DiplomacyState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    state.Treaties.Add(id, t);
                }

                state.ProposalIds = new StableIdAllocator(r.ReadUInt64());
                int proposals = r.ReadCount(r.Remaining);
                for (int i = 0; i < proposals; i++)
                {
                    var id = new ProposalId(r.ReadUInt64());
                    byte kind = r.ReadByte();
                    var p = new Proposal(id, (ProposalKind)kind, new CountryId(r.ReadUInt64()), new CountryId(r.ReadUInt64()), r.ReadDate(), r.ReadDate());
                    byte status = r.ReadByte();
                    if (!id.IsValid || id.Value >= state.ProposalIds.NextValue || kind < (byte)ProposalKind.Peace || kind > (byte)ProposalKind.Alliance
                        || status < (byte)ProposalStatus.Pending || status > (byte)ProposalStatus.Expired)
                    {
                        return Result<DiplomacyState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    p.Status = (ProposalStatus)status;
                    int transfers = r.ReadCount(r.Remaining);
                    for (int t = 0; t < transfers; t++)
                    {
                        p.TransferList.Add(new RegionTransfer(new RegionId(r.ReadUInt64()), new CountryId(r.ReadUInt64())));
                    }

                    state.Proposals.Add(id, p);
                }

                int exiles = r.ReadCount(r.Remaining);
                for (int i = 0; i < exiles; i++)
                {
                    if (!state.Exiles.TryAdd(new CountryId(r.ReadUInt64()), new CountryId(r.ReadUInt64())))
                    {
                        return Result<DiplomacyState>.Failure(DomainErrorKeys.KayitBozuk);
                    }
                }

                state.RebuildIndex();
                return Result<DiplomacyState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<DiplomacyState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
