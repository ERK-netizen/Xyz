using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

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
    }
}
