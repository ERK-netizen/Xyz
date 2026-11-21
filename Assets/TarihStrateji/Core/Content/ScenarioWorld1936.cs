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
    /// "Dünya 1936" senaryosu (Adım 21; 7 Mart 1936, tam dünya; coğrafya <c>dunya1936</c> paketinden): CShapes 2.0'ın o gün
    /// geçerli 170 birimi (devletler, sömürgeler, mandalar) 71 egemen devlete bağlıdır, 418 bölge/şehir. Ortak kurulum
    /// <see cref="PackageScenario"/>; Türkiye tek parti cumhuriyetidir (CHP). YZ'nin ilk savaş ilanı Marco Polo Köprüsü
    /// olayına (07.07.1937, Çin–Japon Savaşı) kadar ertelenir.
    /// </summary>
    public static class ScenarioWorld1936
    {
        /// <summary>Oyuncu varsayılanı: Türkiye Cumhuriyeti.</summary>
        public static readonly CountryId Turkey = BaselineStatecraft.Turkey;

        /// <summary>YZ'nin ilk savaş ilanı için en erken tarih: Marco Polo Köprüsü olayı (07.07.1937).</summary>
        public static readonly GameDate AiEarliestWar = GameDate.FromYmd(1937, 7, 7);

        /// <summary>
        /// 71 egemen devletin 1936 başkentleri (ülke kimliği sırasıyla; ör. Ankara, Rio de Janeiro, Nankin, Kaunas). Sömürge ve
        /// manda merkezleri yöneten devletin başkenti değildir.
        /// </summary>
        public static readonly string[] CapitalCityKeys =
        {
            "sehir.d1936.ankara", "sehir.d1936.atina", "sehir.d1936.sofya", "sehir.d1936.belgrad", "sehir.d1936.kahire", "sehir.d1936.bagdat",
            "sehir.d1936.bukres", "sehir.d1936.viyana", "sehir.d1936.roma", "sehir.d1936.moskova", "sehir.d1936.londra", "sehir.d1936.tahran",
            "sehir.d1936.paris", "sehir.d1936.budapeste", "sehir.d1936.tirane", "sehir.d1936.berlin", "sehir.d1936.madrid", "sehir.d1936.lizbon",
            "sehir.d1936.amsterdam", "sehir.d1936.bruksel", "sehir.d1936.varsova", "sehir.d1936.prag", "sehir.d1936.bern", "sehir.d1936.kopenhag",
            "sehir.d1936.stockholm", "sehir.d1936.oslo", "sehir.d1936.helsinki", "sehir.d1936.tallinn", "sehir.d1936.dublin", "sehir.d1936.washington",
            "sehir.d1936.ottava", "sehir.d1936.meksiko", "sehir.d1936.havana", "sehir.d1936.guatemalacity", "sehir.d1936.bogota", "sehir.d1936.lima",
            "sehir.d1936.riodejaneiro", "sehir.d1936.buenosaires", "sehir.d1936.santiago", "sehir.d1936.lapaz", "sehir.d1936.montevideo", "sehir.d1936.monrovia",
            "sehir.d1936.addisababa", "sehir.d1936.pretorya", "sehir.d1936.riyad", "sehir.d1936.sana", "sehir.d1936.kabil", "sehir.d1936.nankin",
            "sehir.d1936.ulanbator", "sehir.d1936.tokyo", "sehir.d1936.katmandu", "sehir.d1936.bangkok", "sehir.d1936.canberra", "sehir.d1936.wellington",
            "sehir.d1936.riga", "sehir.d1936.kaunas", "sehir.d1936.portauprince", "sehir.d1936.santodomingo", "sehir.d1936.tegucigalpa", "sehir.d1936.sansalvador",
            "sehir.d1936.managua", "sehir.d1936.sanjose", "sehir.d1936.panama", "sehir.d1936.karakas", "sehir.d1936.kito", "sehir.d1936.asuncion",
            "sehir.d1936.maskat", "sehir.d1936.thimphu", "sehir.d1936.luksemburg", "sehir.d1936.reykjavik", "sehir.d1936.lhasa",
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
                throw new InvalidOperationException("Dünya 1936 senaryosu siyaseti kurulamadı.");
            }
        }
    }
}
