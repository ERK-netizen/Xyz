using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Hashing;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content.Packages
{
    /// <summary>
    /// TSGP sürüm 1 okuyucusu (yazıcı: <c>Tools/ContentCompiler/compile_geo_package.py</c>). Biçim
    /// <see cref="CanonicalWriter"/> ile aynıdır (little-endian, uzunluk önekli UTF-8); son 32 bayt yükün
    /// SHA-256'sıdır. Sıra: başlık (etiket, sürüm, senaryo, tarih) → kaynaklar → ülkeler → bölgeler (sahip,
    /// ad, nüfus, çokgenler; halka noktaları enlem/boylam E6) → kara sınırları → düğümler → kenarlar →
    /// şehirler. Okuma sonunda dünya bir kez kurulup doğrulanır (geometri, graf ve şehir kuralları).
    /// </summary>
    public static class GeoPackageReader
    {
        public const uint Tag = 0x50475354; // "TSGP"
        public const int Version = 1;
        private const int HashLength = 32;

        public static Result<GeoPackage> Read(byte[] data)
        {
            if (data == null || data.Length <= HashLength)
            {
                return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
            }

            int payloadLength = data.Length - HashLength;
            byte[] expected = Sha256.Compute(data, 0, payloadLength);
            var actual = new byte[HashLength];
            Array.Copy(data, payloadLength, actual, 0, HashLength);
            if (!Sha256.AreEqual(expected, actual))
            {
                return Result<GeoPackage>.Failure(ContentErrorKeys.PaketOzetiUyusmuyor);
            }

            var payload = new byte[payloadLength];
            Array.Copy(data, payload, payloadLength);
            try
            {
                var r = new CanonicalReader(payload);
                r.ExpectTag(Tag);
                if (r.ReadInt32() != Version)
                {
                    return Result<GeoPackage>.Failure(ContentErrorKeys.PaketSurumuDesteklenmiyor);
                }

                string scenario = r.ReadString();
                int year = r.ReadInt32();
                int month = r.ReadInt32();
                int day = r.ReadInt32();
                if (!GameDate.IsValid(year, month, day))
                {
                    return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                }

                var sources = new PackageSource[r.ReadCount(r.Remaining)];
                for (int i = 0; i < sources.Length; i++)
                {
                    sources[i] = new PackageSource(r.ReadString(), r.ReadString(), r.ReadString(), r.ReadString(), r.ReadString());
                }

                var countries = new CountryId[r.ReadCount(r.Remaining)];
                var countryNames = new string[countries.Length];
                for (int i = 0; i < countries.Length; i++)
                {
                    countries[i] = new CountryId(r.ReadUInt64());
                    countryNames[i] = r.ReadString();
                    if (i > 0 && countries[i - 1] >= countries[i])
                    {
                        return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                    }
                }

                var regions = new PackageRegion[r.ReadCount(r.Remaining)];
                for (int i = 0; i < regions.Length; i++)
                {
                    var id = new RegionId(r.ReadUInt64());
                    var owner = new CountryId(r.ReadUInt64());
                    string name = r.ReadString();
                    long population = r.ReadInt64();
                    var polygons = new GeoPolygon[r.ReadCount(r.Remaining)];
                    for (int p = 0; p < polygons.Length; p++)
                    {
                        int ringCount = r.ReadCount(r.Remaining);
                        if (ringCount < 1)
                        {
                            return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                        }

                        GeoRing outer = ReadRing(r);
                        var holes = new GeoRing[ringCount - 1];
                        for (int h = 0; h < holes.Length; h++)
                        {
                            holes[h] = ReadRing(r);
                        }

                        polygons[p] = new GeoPolygon(outer, holes);
                    }

                    if (population <= 0L || polygons.Length == 0)
                    {
                        return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                    }

                    regions[i] = new PackageRegion(id, owner, name, population, new GeoMultiPolygon(polygons));
                }

                var borders = new (RegionId, RegionId, long, bool)[r.ReadCount(r.Remaining)];
                for (int i = 0; i < borders.Length; i++)
                {
                    borders[i] = (new RegionId(r.ReadUInt64()), new RegionId(r.ReadUInt64()), r.ReadInt64(), r.ReadBool());
                }

                var nodes = new TransportNode[r.ReadCount(r.Remaining)];
                for (int i = 0; i < nodes.Length; i++)
                {
                    var id = new TransportNodeId(r.ReadUInt64());
                    var location = new GeoPoint(r.ReadInt32(), r.ReadInt32());
                    var region = new RegionId(r.ReadUInt64());
                    byte kind = r.ReadByte();
                    if (kind < (byte)TransportNodeKind.Junction || kind > (byte)TransportNodeKind.Sea)
                    {
                        return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                    }

                    nodes[i] = new TransportNode(id, location, region, (TransportNodeKind)kind);
                }

                var edges = new TransportEdge[r.ReadCount(r.Remaining)];
                for (int i = 0; i < edges.Length; i++)
                {
                    var id = new TransportEdgeId(r.ReadUInt64());
                    var a = new TransportNodeId(r.ReadUInt64());
                    var b = new TransportNodeId(r.ReadUInt64());
                    byte kind = r.ReadByte();
                    long length = r.ReadInt64();
                    int capacity = r.ReadInt32();
                    byte terrain = r.ReadByte();
                    if (kind < (byte)TransportEdgeKind.Track || kind > (byte)TransportEdgeKind.StraitCrossing || terrain >= TerrainRules.TerrainTypeCount)
                    {
                        return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                    }

                    edges[i] = new TransportEdge(id, a, b, (TransportEdgeKind)kind, length, capacity, GameDate.MinValue, terrain);
                }

                var cities = new PackageCity[r.ReadCount(r.Remaining)];
                for (int i = 0; i < cities.Length; i++)
                {
                    var id = new CityId(r.ReadUInt64());
                    string name = r.ReadString();
                    var location = new GeoPoint(r.ReadInt32(), r.ReadInt32());
                    var region = new RegionId(r.ReadUInt64());
                    var node = new TransportNodeId(r.ReadUInt64());
                    int tier = r.ReadInt32();
                    bool port = r.ReadBool();
                    var owner = new CountryId(r.ReadUInt64());
                    if (tier < 1 || tier > 5)
                    {
                        return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
                    }

                    cities[i] = new PackageCity(id, name, location, region, node, tier, port, owner);
                }

                r.ExpectEnd();
                var package = new GeoPackage(scenario, GameDate.FromYmd(year, month, day), sources, countries, countryNames, regions, borders, nodes, edges, cities);
                Result<GeoWorld> world = package.TryBuildWorld();
                return world.IsSuccess ? Result<GeoPackage>.Success(package) : Result<GeoPackage>.Failure(world.ErrorKey);
            }
            catch (FormatException)
            {
                return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
            }
            catch (ArgumentException)
            {
                return Result<GeoPackage>.Failure(ContentErrorKeys.PaketBozuk);
            }
        }

        private static GeoRing ReadRing(CanonicalReader r)
        {
            var points = new GeoPoint[r.ReadCount(r.Remaining / 8)];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new GeoPoint(r.ReadInt32(), r.ReadInt32());
            }

            return new GeoRing(points);
        }
    }
}
