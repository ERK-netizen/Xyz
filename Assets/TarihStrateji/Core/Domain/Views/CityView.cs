using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>Sunum için şehir görünümü.</summary>
    public readonly struct CityView
    {
        public CityView(CityId id, int cellIndex, string nameKey, byte tier, CountryId owner, CountryId controller, bool isBesieged)
        {
            Id = id;
            CellIndex = cellIndex;
            NameKey = nameKey;
            Tier = tier;
            Owner = owner;
            Controller = controller;
            IsBesieged = isBesieged;
        }

        public CityId Id { get; }

        public int CellIndex { get; }

        public string NameKey { get; }

        public byte Tier { get; }

        public CountryId Owner { get; }

        public CountryId Controller { get; }

        public bool IsBesieged { get; }
    }
}
