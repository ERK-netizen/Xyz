using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>
    /// Senaryo durumlarını kurar (tanım verisi Content'te). Ege 1925: Türkiye 6 tümen (İstanbul,
    /// Ankara, İzmir) ve bir muhrip filotillası (İstanbul); Yunanistan 5 tümen (Atina, Selanik,
    /// Larisa) ve bir muhrip filotillası (Atina). Birlikler ve gemiler gerçek nüfustan ve stoktan
    /// gelir (kurallarla toplanır). Kayıttan yüklemede değişmez dünya <see cref="AegeanScenario.BuildWorld"/>
    /// ile şablon olarak yeniden kurulur (<see cref="SimulationSetup.GeoWorldTemplate"/>).
    /// </summary>
    public static class ScenarioFactory
    {
        public static GameState CreateAegean(GameContent content, ulong seed, GameMode mode, CountryId player)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            GameState state = GameState.CreateNew(seed, mode, AegeanScenario.Start, mode == GameMode.Observer ? CountryId.None : player, AegeanScenario.BuildWorld());
            AegeanScenario.Populate(state.World, state.Population, state.Economy, state.Research, state.Politics, state.Characters, state.Effects, content, state.Today);
            GeoWarfareContext war = GeoWarfareContexts.From(state, content);
            Army(war, state, BaselineStatecraft.Turkey, new[] { "sehir.ege.istanbul", "sehir.ege.istanbul", "sehir.ege.ankara", "sehir.ege.ankara", "sehir.ege.izmir", "sehir.ege.izmir" });
            Army(war, state, BaselineStatecraft.Greece, new[] { "sehir.ege.atina", "sehir.ege.atina", "sehir.ege.selanik", "sehir.ege.selanik", "sehir.ege.larisa" });
            Require(GeoNavalRules.Build(war, BaselineStatecraft.Turkey, BaselineUnits.DestroyerFlotilla, CityOf(state, "sehir.ege.istanbul")).IsSuccess);
            Require(GeoNavalRules.Build(war, BaselineStatecraft.Greece, BaselineUnits.DestroyerFlotilla, CityOf(state, "sehir.ege.atina")).IsSuccess);
            return state;
        }

        /// <summary>
        /// "1914 MVP" (28 Haziran 1914, barış): coğrafya paketten; her ülkeye nüfusuna göre 1–10 tümen
        /// (büyükten küçüğe şehirlerine sırayla dağıtılır, toplanabilen şehirlerde) ve ana liman sahibi
        /// büyük güçlere birer muhrip filotillası.
        /// </summary>
        public static GameState Create1914(GameContent content, GeoPackage package, ulong seed, GameMode mode, CountryId player) =>
            CreateFromPackage(content, package, seed, mode, player, Scenario1914.Populate);

        /// <summary>"1936" (7 Mart 1936): 1914 ile aynı ordu/filo kuralıyla, <see cref="Scenario1936"/> kurulumu.</summary>
        public static GameState Create1936(GameContent content, GeoPackage package, ulong seed, GameMode mode, CountryId player) =>
            CreateFromPackage(content, package, seed, mode, player, Scenario1936.Populate);

        /// <summary>"Dünya 1936" (7 Mart 1936, tam dünya, 71 devlet): <see cref="ScenarioWorld1936"/> kurulumu.</summary>
        public static GameState CreateWorld1936(GameContent content, GeoPackage package, ulong seed, GameMode mode, CountryId player) =>
            CreateFromPackage(content, package, seed, mode, player, ScenarioWorld1936.Populate);

        /// <summary>"1939" (1 Eylül 1939): 1914 ile aynı ordu/filo kuralıyla, <see cref="Scenario1939"/> kurulumu.</summary>
        public static GameState Create1939(GameContent content, GeoPackage package, ulong seed, GameMode mode, CountryId player) =>
            CreateFromPackage(content, package, seed, mode, player, Scenario1939.Populate);

        /// <summary>Paket senaryosu kurucusu (<see cref="Scenario1914.Populate"/> imzasındaki doldurucu).</summary>
        public delegate void PackagePopulator(GeoPackage package, GeoWorld world, Population.PopulationState population, Economy.State.EconomyState economy,
            Research.State.ResearchState research, Politics.State.PoliticsState politics, Characters.State.CharacterState characters,
            Effects.EffectEngine effects, GameContent content, Domain.Time.GameDate today);

        public static GameState CreateFromPackage(GameContent content, GeoPackage package, ulong seed, GameMode mode, CountryId player, PackagePopulator populate)
        {
            if (content == null || package == null || populate == null)
            {
                throw new ArgumentNullException(content == null ? nameof(content) : package == null ? nameof(package) : nameof(populate));
            }

            GameState state = GameState.CreateNew(seed, mode, package.Date, mode == GameMode.Observer ? CountryId.None : player, package.BuildWorld());
            populate(package, state.World, state.Population, state.Economy, state.Research, state.Politics, state.Characters, state.Effects, content, state.Today);
            GeoWarfareContext war = GeoWarfareContexts.From(state, content);
            for (int i = 0; i < package.Countries.Count; i++)
            {
                CountryId country = package.Countries[i];
                int divisions = PackageScenario.Divisions(PackageScenario.CountryPopulation(package, country));
                var sites = new List<PackageCity>();
                for (int c = 0; c < package.Cities.Count; c++)
                {
                    if (package.Cities[c].Owner == country)
                    {
                        sites.Add(package.Cities[c]);
                    }
                }

                // Büyük şehir önce (kademe azalan, eşitlikte kimlik sırası; kararlı sıralama).
                sites.Sort((a, b) => a.Tier != b.Tier ? b.Tier.CompareTo(a.Tier) : a.Id.CompareTo(b.Id));
                int placed = 0;
                for (int round = 0; round < 4 && placed < divisions; round++)
                {
                    for (int c = 0; c < sites.Count && placed < divisions; c++)
                    {
                        if (GeoUnitRules.Recruit(war, country, BaselineUnits.InfantryDivision, sites[c].Id).IsSuccess)
                        {
                            placed++;
                        }
                    }
                }

                for (int c = 0; c < sites.Count; c++)
                {
                    if (sites[c].IsPort && sites[c].Tier >= 4
                        && GeoNavalRules.Build(war, country, BaselineUnits.DestroyerFlotilla, sites[c].Id).IsSuccess)
                    {
                        break;
                    }
                }
            }

            return state;
        }

        private static void Army(GeoWarfareContext war, GameState state, CountryId country, string[] cities)
        {
            for (int i = 0; i < cities.Length; i++)
            {
                Require(GeoUnitRules.Recruit(war, country, BaselineUnits.InfantryDivision, CityOf(state, cities[i])).IsSuccess);
            }
        }

        public static CityId CityOf(GameState state, string cityKey)
        {
            for (int i = 0; i < state.World.Cities.Count; i++)
            {
                state.World.TryGetCity(state.World.Cities[i], out City city);
                if (string.Equals(city.NameKey, cityKey, StringComparison.Ordinal))
                {
                    return city.Id;
                }
            }

            throw new InvalidOperationException("Senaryo şehri yok: " + cityKey);
        }

        private static void Require(bool ok)
        {
            if (!ok)
            {
                throw new InvalidOperationException("Senaryo birlikleri kurulamadı.");
            }
        }
    }
}
