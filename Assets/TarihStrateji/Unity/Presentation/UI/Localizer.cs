using System;
using System.Collections.Generic;
using UnityEngine;

namespace TarihStrateji.Unity.Presentation.UI
{
    /// <summary>
    /// Türkçe metin tablosu (Resources/Localization/tr.txt, satır biçimi "anahtar = metin", '#' yorum).
    /// Kural kodu yalnız anahtar üretir (ör. <c>hata.emir.menzilDisi</c>); metne çeviri burada olur.
    /// Bulunmayan anahtar olduğu gibi gösterilir (eksik çeviri görünür kalsın diye).
    /// </summary>
    public sealed class Localizer
    {
        public const string TurkishResource = "Localization/tr";

        private readonly Dictionary<string, string> _table;

        private Localizer(Dictionary<string, string> table)
        {
            _table = table;
        }

        public int Count => _table.Count;

        public static Localizer LoadTurkish()
        {
            TextAsset asset = Resources.Load<TextAsset>(TurkishResource);
            if (asset == null)
            {
                Debug.LogError("Türkçe metin tablosu bulunamadı: Resources/" + TurkishResource);
                return Parse(string.Empty);
            }

            return Parse(asset.text);
        }

        public static Localizer Parse(string text)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int separator = line.IndexOf(" = ", StringComparison.Ordinal);
                if (separator <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 3).Trim();
                if (value.Length > 0)
                {
                    table[key] = value;
                }
            }

            return new Localizer(table);
        }

        public bool Contains(string key) => key != null && _table.ContainsKey(key);

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return _table.TryGetValue(key, out string value) ? value : key;
        }
    }
}
