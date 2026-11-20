using System;
using TarihStrateji.Core.Content;
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
