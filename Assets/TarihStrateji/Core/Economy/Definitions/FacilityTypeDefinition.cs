using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Definitions
{
    /// <summary>
    /// Tesis türü (içerik verisi). Girdi/çıktı miktarları tam kadro ve %100 verimde günlük
    /// değerlerdir. Şehirde yapı slotu kullanır; altyapı kullanmaz (CLAUDE.md §2.3).
    /// <see cref="RequiresPort"/> tesis yalnız liman şehrine kurulur (tersane).
    /// </summary>
    public sealed class FacilityTypeDefinition
    {
        private readonly GoodAmount[] _inputs;
        private readonly GoodAmount[] _outputs;
        private readonly GoodAmount[] _buildGoods;

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
            bool requiresPort)
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
            RequiresPort = requiresPort;
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

        public bool RequiresPort { get; }
    }
}
