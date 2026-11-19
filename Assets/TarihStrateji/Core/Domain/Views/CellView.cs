using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>Sunum için hücre görünümü (salt okunur kopya). <see cref="Terrain"/> arazi türünün sayısal kodudur.</summary>
    public readonly struct CellView
    {
        public CellView(byte terrain, bool isWater, CountryId owner, CountryId controller, CityId city)
        {
            Terrain = terrain;
            IsWater = isWater;
            Owner = owner;
            Controller = controller;
            City = city;
        }

        public byte Terrain { get; }

        public bool IsWater { get; }

        public CountryId Owner { get; }

        public CountryId Controller { get; }

        public CityId City { get; }
    }
}
