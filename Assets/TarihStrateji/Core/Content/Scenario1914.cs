using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters.State;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Economy.Geo;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Economy.State;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Politics.State;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Research.State;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// "1914 MVP" senaryosunun durum kurulumu (Balkanlar–Anadolu–Doğu Akdeniz, 28 Haziran 1914). Coğrafya
    /// <see cref="GeoPackage"/>'tan gelir (ContentCompiler); burada her paket ülkesine ekonomi, araştırma ve
    /// karakter durumu, bölgelere nüfus, şehirlere tesis yerleştirilir. Siyaset partisi tanımlı ülkeler
    /// (Osmanlı: İttihat ve Terakki iktidarda, Hürriyet ve İtilaf muhalefette; Yunanistan: Liberal) meşruti
    /// monarşiyle başlar; diğerlerinin siyaset durumu yoktur (istikrar 1 sayılır). Sayılar temsilîdir:
    /// hazine nüfusla, stoklar ordu büyüklüğüyle ölçeklenir; nüfus paketteki 1/5 ölçekli değerdir.
    /// </summary>
    public static class Scenario1914
    {
        /// <summary>Oyuncu varsayılanı: Osmanlı İmparatorluğu.</summary>
        public static readonly CountryId Ottoman = BaselineStatecraft.Turkey;

        /// <summary>Ölçekli nüfusun bu kadar kişisine bir tümen düşer (başlangıç ordusu).</summary>
        public const long PeoplePerDivision = PackageScenario.PeoplePerDivision;

        /// <summary>
        /// YZ'nin ilk savaş ilanı için en erken tarih: Temmuz Krizi (Saraybosna 28.06 → Avusturya-Macaristan'ın
        /// Sırbistan'a savaş ilanı 28.07.1914). Adım 17 denge taramasında YZ her tohumda 6. gün savaş ilan ediyordu.
        /// </summary>
        public static readonly GameDate AiEarliestWar = GameDate.FromYmd(1914, 7, 28);

        /// <summary>
        /// 28 Haziran 1914 başkentleri (kapsam içindekiler): İstanbul, Atina, Sofya, Belgrad, Çetine, Dıraç
        /// (Prens Wied'in merkezi), Bükreş, Roma. Viyana, Petersburg, Londra ve Tahran kapsam dışındadır; bu
        /// ülkelerin haritada başkenti yoktur.
        /// </summary>
        public static readonly string[] CapitalCityKeys =
        {
            "sehir.1914.istanbul", "sehir.1914.atina", "sehir.1914.sofya", "sehir.1914.belgrad",
            "sehir.1914.cetine", "sehir.1914.drac", "sehir.1914.bukres", "sehir.1914.roma",
        };

        public static void Populate(GeoPackage package, GeoWorld world, PopulationState population, EconomyState economy, ResearchState research,
            PoliticsState politics, CharacterState characters, EffectEngine effects, GameContent content, GameDate today) =>
            PackageScenario.Populate(package, world, population, economy, research, politics, characters, effects, content, today, SetupPolitics);

        /// <summary>Osmanlı (İttihat ve Terakki iktidar, Hürriyet ve İtilaf muhalefet) ve Yunanistan (Liberal) meşruti monarşi.</summary>
        private static void SetupPolitics(PoliticsState politics, GameContent content, GameDate today)
        {
            Require(politics.AddCountry(Ottoman, content.Politics, BaselineStatecraft.ConstitutionalMonarchy, BaselineStatecraft.UnionAndProgress,
                new[] { Pair(BaselineStatecraft.UnionAndProgress, 7), Pair(BaselineStatecraft.FreedomAndAccord, 3) }, Fixed64.Parse("0.45"), today).IsSuccess);
            Require(politics.AddCountry(BaselineStatecraft.Greece, content.Politics, BaselineStatecraft.ConstitutionalMonarchy, BaselineStatecraft.GreekLiberal,
                new[] { Pair(BaselineStatecraft.GreekLiberal, 6), Pair(BaselineStatecraft.GreekPeoples, 4) }, Fixed64.Parse("0.55"), today).IsSuccess);
        }

        public static long CountryPopulation(GeoPackage package, CountryId country) => PackageScenario.CountryPopulation(package, country);

        public static int Divisions(long people) => PackageScenario.Divisions(people);

        private static KeyValuePair<PartyId, Fixed64> Pair(PartyId party, int weight) => new KeyValuePair<PartyId, Fixed64>(party, Fixed64.FromInt(weight));

        private static void Require(bool ok)
        {
            if (!ok)
            {
                throw new InvalidOperationException("1914 senaryosu kurulamadı.");
            }
        }
    }
}
