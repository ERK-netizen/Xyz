using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Coğrafi dünyanın kanonik anlık görüntüsü (ADR-011, §16.4): değişmez geometri kayda yazılmaz, yalnız
    /// SHA-256 parmak izi (bölge/şehir/düğüm/kenar kimlikleri, koordinatlar, uzunluklar) yazılır; yükleme
    /// aynı içerikten kurulmuş şablona uygulanır ve parmak izi uyuşmazsa reddedilir (REG-029, MAP-G09).
    /// Değişken katman: şehir sahip/kontrolcü/gelişim/kuşatma/tahkimat ve bölge kontrol durumu.
    /// </summary>
    public static class GeoWorldSnapshotCodec
    {
        public const uint Tag = 0x574F4547; // "GEOW"

        public static void Write(GeoWorld world, CanonicalWriter w)
        {
            w.WriteTag(Tag);
            w.WriteBytes(StaticFingerprint(world));
            w.WriteInt32(world.Cities.Count);
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City c);
                w.WriteId(c.Id);
                w.WriteId(c.Owner);
                w.WriteId(c.Controller);
                w.WriteInt32(c.DevelopmentLevel);
                w.WriteByte((byte)c.Siege);
                w.WriteFixed64(c.Fortification);
            }

            IReadOnlyList<RegionId> regions = world.Regions;
            w.WriteInt32(regions.Count);
            for (int i = 0; i < regions.Count; i++)
            {
                world.TryGetControl(regions[i], out RegionControl rc);
                w.WriteId(rc.Region);
                w.WriteId(rc.Owner);
                w.WriteId(rc.Controller);
                w.WriteByte((byte)rc.Stage);
                w.WriteId(rc.Challenger);
                w.WriteInt32(rc.DaysHeld);
            }
        }

        /// <summary>Kayıttaki değişken durumu <paramref name="template"/>'e uygular.</summary>
        public static Result<GeoWorld> Read(CanonicalReader r, GeoWorld template)
        {
            if (template == null)
            {
                return Result<GeoWorld>.Failure(GeographyErrorKeys.CografyaSablonuYok);
            }

            try
            {
                r.ExpectTag(Tag);
                byte[] fingerprint = r.ReadBytes();
                if (!SameBytes(fingerprint, StaticFingerprint(template)))
                {
                    return Result<GeoWorld>.Failure(GeographyErrorKeys.CografyaUyumsuz);
                }

                int cities = r.ReadCount(r.Remaining);
                if (cities != template.Cities.Count)
                {
                    return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                }

                for (int i = 0; i < cities; i++)
                {
                    var id = new CityId(r.ReadUInt64());
                    if (id != template.Cities[i] || !template.TryGetCity(id, out City c))
                    {
                        return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    c.Owner = new CountryId(r.ReadUInt64());
                    c.Controller = new CountryId(r.ReadUInt64());
                    c.DevelopmentLevel = r.ReadInt32();
                    byte siege = r.ReadByte();
                    if (siege > (byte)SiegeState.Besieged)
                    {
                        return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    c.Siege = (SiegeState)siege;
                    c.Fortification = r.ReadFixed64();
                }

                int regions = r.ReadCount(r.Remaining);
                if (regions != template.Regions.Count)
                {
                    return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                }

                for (int i = 0; i < regions; i++)
                {
                    var id = new RegionId(r.ReadUInt64());
                    if (id != template.Regions[i] || !template.TryGetControl(id, out RegionControl rc))
                    {
                        return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    rc.Owner = new CountryId(r.ReadUInt64());
                    rc.Controller = new CountryId(r.ReadUInt64());
                    byte stage = r.ReadByte();
                    if (stage > (byte)RegionControlStage.Occupied)
                    {
                        return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
                    }

                    rc.Stage = (RegionControlStage)stage;
                    rc.Challenger = new CountryId(r.ReadUInt64());
                    rc.DaysHeld = r.ReadInt32();
                }

                return Result<GeoWorld>.Success(template);
            }
            catch (ArgumentException)
            {
                return Result<GeoWorld>.Failure(DomainErrorKeys.KayitBozuk);
            }
        }

        /// <summary>Değişmez katmanın SHA-256 özeti (kimlik sırasıyla; kayan nokta yok).</summary>
        public static byte[] StaticFingerprint(GeoWorld world)
        {
            var w = new CanonicalWriter(4096);
            IReadOnlyList<RegionId> regions = world.Regions;
            w.WriteInt32(regions.Count);
            for (int i = 0; i < regions.Count; i++)
            {
                world.Atlas.TryGetShape(regions[i], out GeoMultiPolygon shape);
                w.WriteId(regions[i]);
                w.WriteInt32(shape.Count);
                for (int p = 0; p < shape.Count; p++)
                {
                    WriteRing(w, shape[p].Outer);
                    w.WriteInt32(shape[p].HoleCount);
                    for (int h = 0; h < shape[p].HoleCount; h++)
                    {
                        WriteRing(w, shape[p].Hole(h));
                    }
                }

                IReadOnlyList<BorderLink> links = world.Borders.Neighbors(regions[i]);
                w.WriteInt32(links.Count);
                for (int b = 0; b < links.Count; b++)
                {
                    w.WriteId(links[b].Neighbor);
                    w.WriteInt64(links[b].SharedLengthMeters);
                    w.WriteBool(links[b].LandPassable);
                }
            }

            IReadOnlyList<TransportNodeId> nodes = world.Transport.Nodes;
            w.WriteInt32(nodes.Count);
            for (int i = 0; i < nodes.Count; i++)
            {
                world.Transport.TryGetNode(nodes[i], out TransportNode node);
                w.WriteId(node.Id);
                w.WriteInt32(node.Location.LatitudeE6);
                w.WriteInt32(node.Location.LongitudeE6);
                w.WriteId(node.Region);
                w.WriteByte((byte)node.Kind);
                IReadOnlyList<TransportEdge> edges = world.Transport.EdgesOf(node.Id);
                for (int e = 0; e < edges.Count; e++)
                {
                    if (edges[e].A != node.Id)
                    {
                        continue;
                    }

                    w.WriteId(edges[e].Id);
                    w.WriteId(edges[e].B);
                    w.WriteByte((byte)edges[e].Kind);
                    w.WriteInt64(edges[e].LengthMeters);
                    w.WriteInt32(edges[e].DailyCapacity);
                    w.WriteDate(edges[e].AvailableFrom);
                    w.WriteByte(edges[e].Terrain);
                }
            }

            w.WriteInt32(world.Cities.Count);
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City c);
                w.WriteId(c.Id);
                w.WriteString(c.NameKey);
                w.WriteInt32(c.Location.LatitudeE6);
                w.WriteInt32(c.Location.LongitudeE6);
                w.WriteId(c.Region);
                w.WriteId(c.Node);
            }

            return Sha256.Compute(w.ToArray());
        }

        private static void WriteRing(CanonicalWriter w, GeoRing ring)
        {
            w.WriteInt32(ring.Count);
            for (int i = 0; i < ring.Count; i++)
            {
                w.WriteInt32(ring[i].LatitudeE6);
                w.WriteInt32(ring[i].LongitudeE6);
            }
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
