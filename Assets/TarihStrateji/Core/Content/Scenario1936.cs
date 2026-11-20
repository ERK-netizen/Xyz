using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// "1936" senaryosu (7 Mart 1936, Balkanlar–Anadolu–Doğu Akdeniz; coğrafya <c>1936_mvp</c> paketinden): 15 ülke,
    /// ortak kurulum <see cref="PackageScenario"/>. Arnavutluk ve Avusturya bağımsız, Hatay Fransız mandası Suriye'de,
    /// Onikiadalar İtalyan. Türkiye tek parti cumhuriyetidir (CHP); diğer ülkelerin siyaset durumu yoktur (istikrar 1
    /// sayılır). Bölgede savaş yoktur; YZ'nin ilk savaş ilanı İtalya'nın Arnavutluk'u işgaline (07.04.1939) kadar ertelenir.
    /// </summary>
    public static class Scenario1936
    {
        /// <summary>Oyuncu varsayılanı: Türkiye Cumhuriyeti.</summary>
        public static readonly CountryId Turkey = BaselineStatecraft.Turkey;

        /// <summary>YZ'nin ilk savaş ilanı için en erken tarih: İtalya'nın Arnavutluk'u işgali (07.04.1939).</summary>
        public static readonly GameDate AiEarliestWar = GameDate.FromYmd(1939, 4, 7);

        /// <summary>
        /// 7 Mart 1936 başkentleri (kapsam içindekiler): Ankara, Atina, Sofya, Belgrad, Bükreş, Budapeşte, Kahire, Bağdat,
        /// Roma, Tiran. Viyana, Moskova, Londra, Paris ve Tahran kapsam dışındadır; manda şehirleri (Şam, Beyrut, Kudüs)
        /// yöneten devletin başkenti değildir.
        /// </summary>
        public static readonly string[] CapitalCityKeys =
        {
            "sehir.1936.ankara", "sehir.1936.atina", "sehir.1936.sofya", "sehir.1936.belgrad", "sehir.1936.bukres",
            "sehir.1936.budapeste", "sehir.1936.kahire", "sehir.1936.bagdat", "sehir.1936.roma", "sehir.1936.tiran",
        };

        public static void Populate(GeoPackage package, GeoWorld world, PopulationState population, EconomyState economy, ResearchState research,
            PoliticsState politics, CharacterState characters, EffectEngine effects, GameContent content, GameDate today) =>
            PackageScenario.Populate(package, world, population, economy, research, politics, characters, effects, content, today, SetupPolitics);

        /// <summary>Türkiye: tek parti cumhuriyeti, Cumhuriyet Halk Partisi (1935 seçimleri tek listeyle), istikrar 0,65.</summary>
        private static void SetupPolitics(PoliticsState politics, GameContent content, GameDate today)
        {
            var parties = new[] { new KeyValuePair<PartyId, Fixed64>(BaselineStatecraft.RepublicanPeoples, Fixed64.FromInt(10)) };
            if (!politics.AddCountry(Turkey, content.Politics, BaselineStatecraft.SingleParty, BaselineStatecraft.RepublicanPeoples, parties,
                Fixed64.Parse("0.65"), today).IsSuccess)
            {
                throw new InvalidOperationException("1936 senaryosu siyaseti kurulamadı.");
            }
        }
    }
}
