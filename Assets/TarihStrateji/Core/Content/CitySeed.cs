using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Content
{
    /// <summary>Senaryo şehri: anahtar, pencere içi sütun/satır, ülke, kademe (sektör sayısı), liman ve suya bakan yön.</summary>
    public readonly struct CitySeed
    {
        public CitySeed(string key, int column, int row, CountryId country, int tier, bool isPort, int facing)
        {
            Key = key;
            Column = column;
            Row = row;
            Country = country;
            Tier = tier;
            IsPort = isPort;
            Facing = facing;
        }

        public string Key { get; }

        public int Column { get; }

        public int Row { get; }

        public CountryId Country { get; }

        public int Tier { get; }

        public bool IsPort { get; }

        public int Facing { get; }
    }
}
