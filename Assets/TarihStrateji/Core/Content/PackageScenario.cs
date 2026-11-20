using System;
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
    /// Coğrafi paketten kurulan senaryoların ortak durum kurulumu (1914, 1939): bölgelere yaş piramidiyle nüfus,
    /// her paket ülkesine nüfusla ölçeklenen hazine/stok, araştırma ve karakter durumu, şehirlere kademeye göre
    /// sanayi ve bölge nüfusunu besleyecek kadar çiftlik. Siyaset senaryoya özeldir (<c>politicsSetup</c>);
    /// sıra (nüfus → ülkeler → siyaset → tesisler) kimlik ve hash kararlılığı için sabittir.
    /// </summary>
    public static class PackageScenario
    {
        /// <summary>Ölçekli nüfusun bu kadar kişisine bir tümen düşer (başlangıç ordusu).</summary>
        public const long PeoplePerDivision = 400_000;

        public static void Populate(GeoPackage package, GeoWorld world, PopulationState population, EconomyState economy, ResearchState research,
            PoliticsState politics, CharacterState characters, EffectEngine effects, GameContent content, GameDate today,
            Action<PoliticsState, GameContent, GameDate> politicsSetup)
        {
            if (package == null || world == null)
            {
                throw new ArgumentNullException(package == null ? nameof(package) : nameof(world));
            }

            for (int i = 0; i < package.Regions.Count; i++)
            {
                population.AddRegion(package.Regions[i].Id, BaselineContent.AgePyramid(package.Regions[i].Population));
            }

            for (int i = 0; i < package.Countries.Count; i++)
            {
                CountryId c = package.Countries[i];
                long people = CountryPopulation(package, c);
                int divisions = Divisions(people);
                Require(economy.AddCountry(c, Fixed64.FromInt(20_000 + (int)(people / 100)), Fixed64.Parse("0.2")).IsSuccess);
                economy.SeedStock(c, BaselineGoods.Food, Fixed64.FromInt(10_000 + (int)(people / 50)));
                economy.SeedStock(c, BaselineGoods.InfantryEquipment, Fixed64.FromInt((divisions * 100) + 300));
                economy.SeedStock(c, BaselineGoods.Steel, Fixed64.FromInt(300));
                economy.SeedStock(c, BaselineGoods.Ship, Fixed64.FromInt(8));
                Require(research.AddCountry(c, content.Technology, new TechnologyId[0], effects, today).IsSuccess);
                Require(characters.AddCountry(c).IsSuccess);
            }

            politicsSetup?.Invoke(politics, content, today);

            int slotsPerLevel = content.Economy.Parameters.BuildingSlotsPerDevelopment;
            for (int i = 0; i < package.Cities.Count; i++)
            {
                PlaceFacilities(package, world, economy, content, package.Cities[i], slotsPerLevel);
            }
        }

        public static long CountryPopulation(GeoPackage package, CountryId country)
        {
            long total = 0L;
            for (int i = 0; i < package.Regions.Count; i++)
            {
                if (package.Regions[i].Owner == country)
                {
                    total += package.Regions[i].Population;
                }
            }

            return total;
        }

        /// <summary>
        /// Başlangıç tümen sayısı: ölçekli nüfus / 400 bin, en çok 10. 400 binin altındaki küçük devletler
        /// (Karadağ, Arnavutluk, İran'ın kapsamdaki kısmı) tümensiz başlar: 10 binlik tümen çalışma çağı
        /// nüfuslarının %12 seferberlik tavanını aşar; savunmayı şehir garnizonu yapar.
        /// </summary>
        public static int Divisions(long people)
        {
            long n = people / PeoplePerDivision;
            return n > 10L ? 10 : (int)n;
        }

        /// <summary>
        /// Şehir tesisleri: önce sanayi (kademe ≥ 3: kömür, demir, çelik, silah; kademe 5: top; büyük liman:
        /// tersane), sonra bölge nüfusunu besleyecek kadar çiftlik (çiftlik 25 gıda/gün, 1000 kişi 1 gıda/gün,
        /// %8 pay) kalan slot kadar. Slot yetmezse eksik gıda başlangıç stoğundan ve ticaretten karşılanır.
        /// </summary>
        private static void PlaceFacilities(GeoPackage package, GeoWorld world, EconomyState economy, GameContent content, PackageCity city, int slotsPerLevel)
        {
            if (city.Tier >= 3)
            {
                Place(economy, content, world, city, BaselineFacilities.CoalMine);
                Place(economy, content, world, city, BaselineFacilities.IronMine);
                Place(economy, content, world, city, BaselineFacilities.SteelWorks);
                Place(economy, content, world, city, BaselineFacilities.ArmsFactory);
            }

            if (city.Tier >= 5)
            {
                Place(economy, content, world, city, BaselineFacilities.ArtilleryFactory);
            }

            if (city.IsPort && city.Tier >= 4)
            {
                Place(economy, content, world, city, BaselineFacilities.Shipyard);
            }

            long people = 0L;
            for (int i = 0; i < package.Regions.Count; i++)
            {
                if (package.Regions[i].Id == city.Region)
                {
                    people = package.Regions[i].Population;
                }
            }

            int farms = (int)(((people * 108) / 100 + 24_999) / 25_000);
            world.TryGetCity(city.Id, out City c);
            int free = CitySlotRules.Capacity(c, slotsPerLevel) - EconomyGeography.UsedSlots(economy, content.Economy, city.Id);
            for (int f = 0; f < farms && f < free; f++)
            {
                Place(economy, content, world, city, BaselineFacilities.Farm);
            }
        }

        private static void Place(EconomyState economy, GameContent content, GeoWorld world, PackageCity city, FacilityTypeId type)
        {
            Result<FacilityId> placed = ConstructionRules.PlaceExisting(economy, content.Economy, world, city.Owner, type, city.Id);
            if (!placed.IsSuccess)
            {
                throw new InvalidOperationException("Senaryo tesisi yerleştirilemedi (" + city.NameKey + "): " + placed.ErrorKey);
            }
        }

        private static void Require(bool ok)
        {
            if (!ok)
            {
                throw new InvalidOperationException("Paket senaryosu kurulamadı.");
            }
        }
    }
}
