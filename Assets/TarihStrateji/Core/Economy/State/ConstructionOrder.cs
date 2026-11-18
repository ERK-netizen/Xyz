using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.State
{
    /// <summary>İnşaat siparişi: bedeli ve slotları sipariş anında ayrılmıştır.</summary>
    public sealed class ConstructionOrder
    {
        internal ConstructionOrder(ConstructionId id, CountryId country, FacilityTypeId type, SectorId sector)
        {
            Id = id;
            Country = country;
            Type = type;
            Sector = sector;
        }

        public ConstructionId Id { get; }

        public CountryId Country { get; }

        public FacilityTypeId Type { get; }

        public SectorId Sector { get; }

        /// <summary>Biriken inşaat günü (hız etkileriyle kesirli olabilir).</summary>
        public Fixed64 Progress { get; internal set; }
    }
}
