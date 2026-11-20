using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Bekleyen antlaşma teklifi (barış, saldırmazlık, ittifak). Yalnız muhatap (<see cref="To"/>) kabul ya da ret
    /// eder; teklif <see cref="LastDay"/>'den sonra düşer. Tür değerleri çekirdekteki teklif türleriyle aynıdır.
    /// </summary>
    public readonly struct ProposalView
    {
        public const byte KindPeace = 1;
        public const byte KindNonAggression = 2;
        public const byte KindAlliance = 3;

        public ProposalView(ulong id, byte kind, CountryId from, CountryId to, GameDate lastDay, int transferCount)
        {
            Id = id;
            Kind = kind;
            From = from;
            To = to;
            LastDay = lastDay;
            TransferCount = transferCount;
        }

        public ulong Id { get; }

        public byte Kind { get; }

        public CountryId From { get; }

        public CountryId To { get; }

        public GameDate LastDay { get; }

        /// <summary>Barış teklifindeki bölge devri sayısı (beyaz barışta 0).</summary>
        public int TransferCount { get; }
    }
}
