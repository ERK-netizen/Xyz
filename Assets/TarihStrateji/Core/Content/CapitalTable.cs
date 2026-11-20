using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Content
{
    /// <summary>
    /// Senaryonun başkent tablosu (tanım verisi, durum değildir): her ülkenin başkenti senaryo başındaki sahibi
    /// olduğu şehirdir. Şehir ad anahtarlarından değişmez dünya şablonuna göre kurulur; bir ülkeye en çok bir
    /// başkent düşer. Başkent harita kapsamı dışındaysa (ör. 1914'te Viyana, Petersburg) ülkenin başkenti yoktur
    /// (<see cref="CityId.None"/>). Okuma modeli şehrin başkent olup olmadığını "sahibinin başkenti mi" diye
    /// sorar; şehir barışla el değiştirirse yeni sahibin başkenti sayılmaz.
    /// </summary>
    public sealed class CapitalTable
    {
        public const string BaskentSehriYok = "hata.icerik.baskentSehriYok";
        public const string BaskentTekrari = "hata.icerik.baskentTekrari";

        private readonly Dictionary<CountryId, CityId> _capitals;

        private CapitalTable(Dictionary<CountryId, CityId> capitals)
        {
            _capitals = capitals;
        }

        public static readonly CapitalTable Empty = new CapitalTable(new Dictionary<CountryId, CityId>());

        public int Count => _capitals.Count;

        /// <summary>Şehir ad anahtarlarından kurar; anahtar dünyada yoksa ya da bir ülkeye iki başkent düşerse hata.</summary>
        public static CapitalTable FromCityKeys(GeoWorld world, IReadOnlyList<string> cityKeys)
        {
            if (world == null || cityKeys == null)
            {
                throw new ArgumentNullException(world == null ? nameof(world) : nameof(cityKeys));
            }

            var capitals = new Dictionary<CountryId, CityId>();
            for (int k = 0; k < cityKeys.Count; k++)
            {
                bool found = false;
                for (int i = 0; i < world.Cities.Count; i++)
                {
                    world.TryGetCity(world.Cities[i], out City city);
                    if (!string.Equals(city.NameKey, cityKeys[k], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (capitals.ContainsKey(city.Owner))
                    {
                        throw new InvalidOperationException(BaskentTekrari + ": " + cityKeys[k]);
                    }

                    capitals.Add(city.Owner, city.Id);
                    found = true;
                    break;
                }

                if (!found)
                {
                    throw new InvalidOperationException(BaskentSehriYok + ": " + cityKeys[k]);
                }
            }

            return new CapitalTable(capitals);
        }

        /// <summary>Ülkenin başkenti; tanımsızsa <see cref="CityId.None"/>.</summary>
        public CityId Of(CountryId country) => _capitals.TryGetValue(country, out CityId city) ? city : CityId.None;
    }
}
