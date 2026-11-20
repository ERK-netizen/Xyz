using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Intelligence.State;

namespace TarihStrateji.Core.Intelligence
{
    /// <summary>
    /// Kalıcı istihbarat: yalnız temas raporları. Görünür hücreler her gün P14'te yeniden
    /// hesaplanır ve kayda yazılmaz (P15'teki YZ aynı gün güncel görüşü okur).
    /// </summary>
    public static class IntelligenceSnapshotCodec
    {
        public const uint Tag = 0x4C544E49; // "INTL"

        public static void Write(IntelligenceState state, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteInt32(state.Countries.Count);
            for (int i = 0; i < state.Countries.Count; i++)
            {
                ObservedWorld view = state.Countries.ValueAt(i);
                w.WriteId(view.Country);
                w.WriteInt32(view.Contacts.Count);
                for (int c = 0; c < view.Contacts.Count; c++)
                {
                    ContactReport r = view.Contacts[c];
                    w.WriteByte((byte)r.Kind);
                    w.WriteUInt64(r.EntityId);
                    w.WriteId(r.Owner);
                    w.WriteId(r.LastNode);
                    w.WriteFixed64(r.EstimatedStrength);
                    w.WriteBool(r.IsExact);
                    w.WriteDate(r.SeenOn);
                }
            }
        }

        public static Result<IntelligenceState> Read(CanonicalReader r)
        {
            try
            {
                r.ExpectTag(Tag);
                var state = new IntelligenceState();
                int countries = r.ReadCount(r.Remaining);
                for (int i = 0; i < countries; i++)
                {
                    var country = new CountryId(r.ReadUInt64());
                    if (!country.IsValid || state.Countries.ContainsKey(country))
                    {
                        return Result<IntelligenceState>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    ObservedWorld view = state.For(country);
                    int contacts = r.ReadCount(r.Remaining);
                    for (int c = 0; c < contacts; c++)
                    {
                        byte kind = r.ReadByte();
                        if (kind < (byte)SightingKind.LandUnit || kind > (byte)SightingKind.AirWing)
                        {
                            return Result<IntelligenceState>.Failure(DomainErrorKeys.KayitBozuk);
                        }

                        var report = new ContactReport((SightingKind)kind, r.ReadUInt64(), new CountryId(r.ReadUInt64()))
                        {
                            LastNode = new TransportNodeId(r.ReadUInt64()),
                            EstimatedStrength = r.ReadFixed64(),
                            IsExact = r.ReadBool(),
                            SeenOn = r.ReadDate(),
                        };
                        if (view.ContactList.Count > 0 && view.ContactList[view.ContactList.Count - 1].SortKey >= report.SortKey)
                        {
                            return Result<IntelligenceState>.Failure(DomainErrorKeys.KayitBozuk);
                        }

                        view.ContactList.Add(report);
                    }
                }

                return Result<IntelligenceState>.Success(state);
            }
            catch (ArgumentException)
            {
                return Result<IntelligenceState>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }
    }
}
