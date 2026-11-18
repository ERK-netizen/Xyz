using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Cities
{
    /// <summary>
    /// Şehir alt sektörü. Sahip ve kontrolcü ayrıdır; bina slotları yalnız burada tutulur ve
    /// altyapıdan bağımsızdır (CLAUDE.md §2.3).
    /// </summary>
    public sealed class CitySector
    {
        internal CitySector(SectorId id, CityId city, SectorSlot slot, SectorKind kind, int buildingSlotCapacity)
        {
            Id = id;
            City = city;
            Slot = slot;
            Kind = kind;
            BuildingSlotCapacity = buildingSlotCapacity;
        }

        public SectorId Id { get; }

        public CityId City { get; }

        public SectorSlot Slot { get; }

        public SectorKind Kind { get; }

        public CountryId Owner { get; internal set; }

        public CountryId Controller { get; internal set; }

        public int BuildingSlotCapacity { get; }

        public int BuildingSlotsUsed { get; internal set; }

        public int FreeBuildingSlots => BuildingSlotCapacity - BuildingSlotsUsed;
    }
}
