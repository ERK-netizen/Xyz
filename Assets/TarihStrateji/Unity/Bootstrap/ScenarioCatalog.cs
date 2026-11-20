using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Content.Packages;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Domain.Views;
using TarihStrateji.Core.Simulation;
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
            public Entry(string id, string titleKey, string packageResource, ScenarioFactory.PackagePopulator populate, string[] capitalCityKeys,
                GameDate aiEarliestWar, CountryId defaultPlayer)
            {
                Id = id;
                TitleKey = titleKey;
                PackageResource = packageResource;
                Populate = populate;
                CapitalCityKeys = capitalCityKeys;
                AiEarliestWar = aiEarliestWar;
                DefaultPlayer = defaultPlayer;
            }

            public string Id { get; }

            public string TitleKey { get; }

            public string PackageResource { get; }

            public ScenarioFactory.PackagePopulator Populate { get; }

            public string[] CapitalCityKeys { get; }

            public GameDate AiEarliestWar { get; }

            public CountryId DefaultPlayer { get; }
        }

        private readonly List<Entry> _entries = new List<Entry>
        {
            new Entry("1914", "senaryo.1914", "Senaryolar/1914_mvp", Scenario1914.Populate, Scenario1914.CapitalCityKeys, Scenario1914.AiEarliestWar, Scenario1914.Ottoman),
            new Entry("1939", "senaryo.1939", "Senaryolar/1939_mvp", Scenario1939.Populate, Scenario1939.CapitalCityKeys, Scenario1939.AiEarliestWar, Scenario1939.Turkey),
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
