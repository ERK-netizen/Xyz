using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Yeni oyunda seçilebilen senaryo (sunum içindir): kimlik ("1914", "1939"), başlık anahtarı, başlangıç tarihi,
    /// varsayılan oyuncu ve seçilebilir ülkeler (kimlik + ad anahtarı, kimlik sırasıyla).
    /// </summary>
    public sealed class ScenarioView
    {
        public ScenarioView(string id, string titleKey, GameDate date, CountryId defaultPlayer, IReadOnlyList<CountryId> countries, IReadOnlyList<string> countryNameKeys)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            TitleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            Date = date;
            DefaultPlayer = defaultPlayer;
            Countries = countries ?? throw new ArgumentNullException(nameof(countries));
            CountryNameKeys = countryNameKeys ?? throw new ArgumentNullException(nameof(countryNameKeys));
            if (countries.Count != countryNameKeys.Count)
            {
                throw new ArgumentException("Ülke ve ad anahtarı sayıları eşit olmalı.", nameof(countryNameKeys));
            }
        }

        public string Id { get; }

        public string TitleKey { get; }

        public GameDate Date { get; }

        public CountryId DefaultPlayer { get; }

        public IReadOnlyList<CountryId> Countries { get; }

        public IReadOnlyList<string> CountryNameKeys { get; }
    }
}
