using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content.Packages
{
    /// <summary>
    /// ContentCompiler'ın ürettiği coğrafi senaryo paketi (TSGP, ADR-011, §22): değişmez bölge geometrisi,
    /// kara komşuluğu, ulaşım grafı, şehirler, başlangıç sahipleri, ölçekli nüfus ve kaynak envanteri.
    /// Okuma sırasında bir kez doğrulanır; <see cref="BuildWorld"/> her çağrıda taze (değişken durumu
    /// başlangıçta) bir <see cref="GeoWorld"/> kurar: yeni oyun ve kayıttan yükleme şablonu aynı veriden gelir.
    /// </summary>
    public sealed class GeoPackage
    {
        private readonly PackageRegion[] _regions;
        private readonly (RegionId A, RegionId B, long Meters, bool Land)[] _borders;
        private readonly TransportNode[] _nodes;
        private readonly TransportEdge[] _edges;
        private readonly PackageCity[] _cities;
        private readonly CountryId[] _countries;
        private readonly string[] _countryNames;
        private readonly PackageSource[] _sources;

        internal GeoPackage(string scenario, GameDate date, PackageSource[] sources, CountryId[] countries, string[] countryNames, PackageRegion[] regions,
            (RegionId, RegionId, long, bool)[] borders, TransportNode[] nodes, TransportEdge[] edges, PackageCity[] cities)
        {
            Scenario = scenario;
            Date = date;
            _sources = sources;
            _countries = countries;
            _countryNames = countryNames;
            _regions = regions;
            _borders = borders;
            _nodes = nodes;
            _edges = edges;
            _cities = cities;
        }

        public string Scenario { get; }

        public GameDate Date { get; }

        public IReadOnlyList<PackageSource> Sources => _sources;

        /// <summary>Paketteki ülkeler (kimlik sırasıyla).</summary>
        public IReadOnlyList<CountryId> Countries => _countries;

        /// <summary>Ülkenin senaryodaki ad anahtarı (ör. <c>ulke.1914.osmanli</c>); pakette yoksa <c>ulke.N</c>.</summary>
        public string CountryNameKey(CountryId country)
        {
            for (int i = 0; i < _countries.Length; i++)
            {
                if (_countries[i] == country)
                {
                    return _countryNames[i];
                }
            }

            return "ulke." + country.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public IReadOnlyList<PackageRegion> Regions => _regions;

        public IReadOnlyList<PackageCity> Cities => _cities;

        public int NodeCount => _nodes.Length;

        public int EdgeCount => _edges.Length;

        /// <summary>Kademe → gelişim seviyesi (5 → 7, 4 → 6, 3 → 4, 2 → 3, 1 → 2).</summary>
        public static int Development(int tier) => tier >= 5 ? 7 : tier == 4 ? 6 : tier == 3 ? 4 : tier == 2 ? 3 : 2;

        /// <summary>Kademe ≥ 4 şehirler tahkimatlıdır (1,5).</summary>
        public static Fixed64 Fortification(int tier) => tier >= 4 ? Fixed64.Parse("1.5") : Fixed64.One;

        public GeoWorld BuildWorld()
        {
            Result<GeoWorld> world = TryBuildWorld();
            if (!world.IsSuccess)
            {
                throw new InvalidOperationException(world.ErrorKey);
            }

            return world.Value;
        }

        internal Result<GeoWorld> TryBuildWorld()
        {
            var world = new GeoWorld();
            for (int i = 0; i < _regions.Length; i++)
            {
                ValidationResult added = world.AddRegion(_regions[i].Id, _regions[i].Shape, _regions[i].Owner);
                if (!added.IsValid)
                {
                    return Result<GeoWorld>.Failure(added.ErrorKey);
                }
            }

            for (int i = 0; i < _borders.Length; i++)
            {
                ValidationResult added = world.Borders.AddBorder(_borders[i].A, _borders[i].B, _borders[i].Meters, _borders[i].Land);
                if (!added.IsValid)
                {
                    return Result<GeoWorld>.Failure(added.ErrorKey);
                }
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                ValidationResult added = world.Transport.AddNode(_nodes[i]);
                if (!added.IsValid)
                {
                    return Result<GeoWorld>.Failure(added.ErrorKey);
                }
            }

            for (int i = 0; i < _edges.Length; i++)
            {
                ValidationResult added = world.Transport.AddEdge(_edges[i]);
                if (!added.IsValid)
                {
                    return Result<GeoWorld>.Failure(added.ErrorKey);
                }
            }

            for (int i = 0; i < _cities.Length; i++)
            {
                PackageCity c = _cities[i];
                var city = new City(c.Id, c.NameKey, c.Location, c.Region, c.Node, Development(c.Tier), c.Owner, Fortification(c.Tier));
                ValidationResult added = world.AddCity(city);
                if (!added.IsValid)
                {
                    return Result<GeoWorld>.Failure(added.ErrorKey);
                }
            }

            ValidationResult valid = world.Validate();
            return valid.IsValid ? Result<GeoWorld>.Success(world) : Result<GeoWorld>.Failure(valid.ErrorKey);
        }
    }
}
