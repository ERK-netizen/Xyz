using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanının dünyayı okuduğu salt-okunur görünüm (CLAUDE.md §2.1, ADR-011). Harita
    /// altıgensizdir: ülke alanları bölge çokgenlerinin sahibe göre birleşimi, şehirler tekil
    /// noktalardır. Hiçbir üye değiştirilebilir durum nesnesi döndürmez.
    /// </summary>
    public interface IGameReadModel
    {
        GameMode Mode { get; }

        GameDate CurrentDate { get; }

        /// <summary>Oyuncunun ülkesi; Gözlemci modunda <see cref="CountryId.None"/>.</summary>
        CountryId PlayerCountry { get; }

        /// <summary>Her durum değişiminde artan sürüm; arayüz önbellekleri için.</summary>
        long StateVersion { get; }

        /// <summary>Durum değiştiğinde (emir kabulü, gün sonu) yeni sürümle tetiklenir.</summary>
        event Action<long> StateChanged;

        /// <summary>Tüm bölgeler (kimlik sırasıyla) sahip/kontrolcü ile <paramref name="into"/>'ya eklenir.</summary>
        void GetRegions(List<RegionView> into);

        /// <summary>Bölgenin değişmez geometrisi (halkalar); bölge yoksa hiçbir şey eklenmez.</summary>
        void GetRegionRings(RegionId region, List<RegionRingView> into);

        /// <summary>Noktayı içeren bölge (dokunma seçimi); yoksa <see cref="RegionId.None"/>.</summary>
        RegionId FindRegion(GeoPoint point);

        /// <summary>Tüm şehirler (kimlik sırasıyla) <paramref name="into"/>'ya eklenir.</summary>
        void GetCities(List<CityView> into);

        bool TryGetCountry(CountryId country, out CountryView view);

        /// <summary>Ülkenin bu senaryodaki ad anahtarı (ör. <c>ulke.1914.osmanli</c>); tanımsızsa <c>ulke.N</c>.</summary>
        string CountryNameKey(CountryId country);

        /// <summary>Ekonomisi olan ülkeler (kimlik sırasıyla).</summary>
        void GetCountries(List<CountryId> into);

        /// <summary>Oyuncunun görebildiği birlikler: kendi birlikleri ve savaş sisindeki temaslar.</summary>
        void GetVisibleUnits(List<UnitView> into);

        /// <summary>Oyuncunun savaşta olduğu ülke mi.</summary>
        bool IsAtWarWith(CountryId other);
    }
}
