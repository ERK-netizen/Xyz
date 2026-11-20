using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Sunum için bölge görünümü (salt okunur kopya). Bölge haritada ayrı çizilmez: ülke alanı,
    /// aynı sahibe ait bölgelerin birleşimidir; iç sınırlar gösterilmez. <see cref="Stage"/> kontrol
    /// aşamasının sayısal kodudur (0 güvenli, 1 çekişmeli, 2 işgal).
    /// </summary>
    public readonly struct RegionView
    {
        public RegionView(RegionId id, CountryId owner, CountryId controller, byte stage)
        {
            Id = id;
            Owner = owner;
            Controller = controller;
            Stage = stage;
        }

        public RegionId Id { get; }

        public CountryId Owner { get; }

        public CountryId Controller { get; }

        public byte Stage { get; }

        public bool IsOccupied => Controller != Owner;
    }
}
