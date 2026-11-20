using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.State;
using UnityEngine;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Oynanabilir paket senaryoları (Adım 18): kimlik, başlık, paket kaynağı, durum kurucusu, başkentler, YZ'nin en erken
    /// savaş tarihi ve varsayılan oyuncu. Paketler ilk istendiğinde <c>Resources</c>'tan okunup doğrulanır ve önbelleğe alınır.
    /// Yeni senaryo eklemek = buraya bir satır + paketi + <c>Scenario*</c> kurulumu.
    /// </summary>
    public sealed class ScenarioCatalog
    {
        public const string DefaultId = "1914";

        public sealed class Entry
        {
            public Entry(string id, string titleKey, string packageResource, Func<GameContent, GeoPackage, ulong, GameMode, CountryId, GameState> create, string[] capitalCityKeys,
                GameDate aiEarliestWar, CountryId defaultPlayer)
            {
                Id = id;
                TitleKey = titleKey;
                PackageResource = packageResource;
                Create = create;
                CapitalCityKeys = capitalCityKeys;
                AiEarliestWar = aiEarliestWar;
                DefaultPlayer = defaultPlayer;
            }

            public string Id { get; }

            public string TitleKey { get; }

            public string PackageResource { get; }

            /// <summary>Senaryo durumu kurucusu (ScenarioFactory.Create1914/Create1936/Create1939; Bootstrap'in başvurduğu türlerle).</summary>
            public Func<GameContent, GeoPackage, ulong, GameMode, CountryId, GameState> Create { get; }

            public string[] CapitalCityKeys { get; }

            public GameDate AiEarliestWar { get; }

            public CountryId DefaultPlayer { get; }
        }

        private readonly List<Entry> _entries = new List<Entry>
        {
            new Entry("1914", "senaryo.1914", "Senaryolar/1914_mvp", ScenarioFactory.Create1914, Scenario1914.CapitalCityKeys, Scenario1914.AiEarliestWar, Scenario1914.Ottoman),
            new Entry("1936", "senaryo.1936", "Senaryolar/1936_mvp", ScenarioFactory.Create1936, Scenario1936.CapitalCityKeys, Scenario1936.AiEarliestWar, Scenario1936.Turkey),
            new Entry("1939", "senaryo.1939", "Senaryolar/1939_mvp", ScenarioFactory.Create1939, Scenario1939.CapitalCityKeys, Scenario1939.AiEarliestWar, Scenario1939.Turkey),
        };

        private readonly Dictionary<string, GeoPackage> _packages = new Dictionary<string, GeoPackage>(StringComparer.Ordinal);

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>Kimliğe göre senaryo; bilinmiyorsa varsayılan (1914).</summary>
        public Entry Find(string id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Id, id, StringComparison.Ordinal))
                {
                    return _entries[i];
                }
            }

            return _entries[0];
        }

        public GeoPackage Package(Entry entry)
        {
            if (_packages.TryGetValue(entry.Id, out GeoPackage cached))
            {
                return cached;
            }

            var asset = Resources.Load<TextAsset>(entry.PackageResource);
            if (asset == null)
            {
                throw new InvalidOperationException("Senaryo paketi bulunamadı: Resources/" + entry.PackageResource);
            }

            Result<GeoPackage> package = GeoPackageReader.Read(asset.bytes);
            if (!package.IsSuccess)
            {
                throw new InvalidOperationException("Senaryo paketi okunamadı (" + entry.Id + "): " + package.ErrorKey);
            }

            _packages[entry.Id] = package.Value;
            return package.Value;
        }

        /// <summary>Sunum için seçim görünümleri (paketler okunur).</summary>
        public void Views(List<ScenarioView> into)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                GeoPackage p = Package(e);
                var names = new string[p.Countries.Count];
                for (int c = 0; c < names.Length; c++)
                {
                    names[c] = p.CountryNameKey(p.Countries[c]);
                }

                into.Add(new ScenarioView(e.Id, e.TitleKey, p.Date, e.DefaultPlayer, p.Countries, names));
            }
        }
    }
}
