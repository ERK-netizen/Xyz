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
    /// "1939" senaryosu (1 Eylül 1939, Balkanlar–Anadolu–Doğu Akdeniz; coğrafya <c>1939_mvp</c> paketinden): 14 ülke,
    /// ortak kurulum <see cref="PackageScenario"/>. Türkiye tek parti cumhuriyetidir (CHP); diğer ülkelerin siyaset durumu
    /// yoktur (istikrar 1 sayılır). Avrupa'da savaş başlamıştır ama bölgedeki devletler henüz tarafsızdır; YZ'nin ilk savaş
    /// ilanı İtalya'nın savaşa girdiği güne (10.06.1940) kadar ertelenir.
    /// </summary>
    public static class Scenario1939
    {
        /// <summary>Oyuncu varsayılanı: Türkiye Cumhuriyeti.</summary>
        public static readonly CountryId Turkey = BaselineStatecraft.Turkey;

        /// <summary>YZ'nin ilk savaş ilanı için en erken tarih: İtalya'nın Fransa ve Britanya'ya savaş ilanı (10.06.1940).</summary>
        public static readonly GameDate AiEarliestWar = GameDate.FromYmd(1940, 6, 10);

        /// <summary>
        /// 1 Eylül 1939 başkentleri (kapsam içindekiler): Ankara, Atina, Sofya, Belgrad, Bükreş, Budapeşte, Kahire, Bağdat,
        /// Roma. Berlin, Moskova, Londra, Paris ve Tahran kapsam dışındadır; manda şehirleri (Şam, Beyrut, Kudüs) yöneten
        /// devletin başkenti değildir.
        /// </summary>
        public static readonly string[] CapitalCityKeys =
        {
            "sehir.1939.ankara", "sehir.1939.atina", "sehir.1939.sofya", "sehir.1939.belgrad", "sehir.1939.bukres",
            "sehir.1939.budapeste", "sehir.1939.kahire", "sehir.1939.bagdat", "sehir.1939.roma",
        };

        public static void Populate(GeoPackage package, GeoWorld world, PopulationState population, EconomyState economy, ResearchState research,
            PoliticsState politics, CharacterState characters, EffectEngine effects, GameContent content, GameDate today) =>
            PackageScenario.Populate(package, world, population, economy, research, politics, characters, effects, content, today, SetupPolitics);

        /// <summary>Türkiye: tek parti cumhuriyeti, Cumhuriyet Halk Partisi (1939 seçimleri tek listeyle), istikrar 0,6.</summary>
        private static void SetupPolitics(PoliticsState politics, GameContent content, GameDate today)
        {
            var parties = new[] { new KeyValuePair<PartyId, Fixed64>(BaselineStatecraft.RepublicanPeoples, Fixed64.FromInt(10)) };
            if (!politics.AddCountry(Turkey, content.Politics, BaselineStatecraft.SingleParty, BaselineStatecraft.RepublicanPeoples, parties,
                Fixed64.Parse("0.6"), today).IsSuccess)
            {
                throw new InvalidOperationException("1939 senaryosu siyaseti kurulamadı.");
            }
        }
    }
}
