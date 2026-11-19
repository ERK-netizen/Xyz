using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// Sunum katmanının dünyayı okuduğu salt-okunur görünüm (CLAUDE.md §2.1). Alan
    /// ayrıntıları (harita, ekonomi, ordu…) sonraki adımlarda bu arayüzü genişleten
    /// salt-okunur görünümlerle eklenir; hiçbiri değiştirilebilir durum nesnesi döndürmez.
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

        /// <summary>Harita genişliği (sütun); hücre dizini = satır × genişlik + sütun.</summary>
        int MapWidth { get; }

        int MapHeight { get; }

        CellView GetCell(int cellIndex);

        /// <summary>Oyuncunun bu hücreyi şu an görüp görmediği (savaş sisi; Gözlemci her şeyi görür).</summary>
        bool IsCellVisible(int cellIndex);

        /// <summary>Tüm şehirler (kimlik sırasıyla) <paramref name="into"/>'ya eklenir.</summary>
        void GetCities(List<CityView> into);

        bool TryGetCountry(CountryId country, out CountryView view);

        /// <summary>Ekonomisi olan ülkeler (kimlik sırasıyla).</summary>
        void GetCountries(List<CountryId> into);

        /// <summary>Oyuncunun görebildiği birlikler: kendi birlikleri ve savaş sisindeki temaslar.</summary>
        void GetVisibleUnits(List<UnitView> into);

        /// <summary>Oyuncunun savaşta olduğu ülke mi.</summary>
        bool IsAtWarWith(CountryId other);
    }
}
