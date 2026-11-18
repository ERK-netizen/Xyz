using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Cities;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>
    /// Tesis türü (içerik verisi). Girdi/çıktı miktarları tam kadro ve %100 verimde günlük
    /// değerlerdir. Bina slotu kullanır; altyapı kullanmaz (CLAUDE.md §2.3).
    /// </summary>
    public sealed class FacilityTypeDefinition
    {
        private readonly GoodAmount[] _inputs;
        private readonly GoodAmount[] _outputs;
        private readonly GoodAmount[] _buildGoods;
        private readonly SectorKind[] _allowedSectors;

        public FacilityTypeDefinition(
            FacilityTypeId id,
            string key,
            GoodAmount[] inputs,
            GoodAmount[] outputs,
            long workersRequired,
            int buildingSlots,
            Fixed64 buildMoney,
            GoodAmount[] buildGoods,
            int buildDays,
            Fixed64 upkeepPerDay,
            SectorKind[] allowedSectors)
        {
            Id = id;
            Key = key;
            _inputs = inputs ?? new GoodAmount[0];
            _outputs = outputs ?? new GoodAmount[0];
            WorkersRequired = workersRequired;
            BuildingSlots = buildingSlots;
            BuildMoney = buildMoney;
            _buildGoods = buildGoods ?? new GoodAmount[0];
            BuildDays = buildDays;
            UpkeepPerDay = upkeepPerDay;
            _allowedSectors = allowedSectors ?? new SectorKind[0];
        }

        public FacilityTypeId Id { get; }

        public string Key { get; }

        public IReadOnlyList<GoodAmount> Inputs => _inputs;

        public IReadOnlyList<GoodAmount> Outputs => _outputs;

        public long WorkersRequired { get; }

        public int BuildingSlots { get; }

        public Fixed64 BuildMoney { get; }

        public IReadOnlyList<GoodAmount> BuildGoods => _buildGoods;

        public int BuildDays { get; }

        public Fixed64 UpkeepPerDay { get; }

        public IReadOnlyList<SectorKind> AllowedSectors => _allowedSectors;

        public bool AllowsSector(SectorKind kind)
        {
            for (int i = 0; i < _allowedSectors.Length; i++)
            {
                if (_allowedSectors[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
