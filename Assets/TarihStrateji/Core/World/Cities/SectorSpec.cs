using System;

namespace TarihStrateji.Core.World.Cities
{
    /// <summary>Şehir kurulurken bir sektörün tanımı (içerik verisinden gelir).</summary>
    [Serializable]
    public readonly struct SectorSpec
    {
        public SectorSpec(SectorKind kind, int buildingSlots)
        {
            if (buildingSlots < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(buildingSlots), WorldErrorKeys.SektorSlotKapasitesi);
            }

            Kind = kind;
            BuildingSlots = buildingSlots;
        }

        public SectorKind Kind { get; }

        public int BuildingSlots { get; }
    }
}
