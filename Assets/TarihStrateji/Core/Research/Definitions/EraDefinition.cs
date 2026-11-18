using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Research.Definitions
{
    /// <summary>
    /// Çağ (içerik verisi). Bir ülke çağa, tarih <see cref="StartDate"/>'e ulaştığında ve
    /// <see cref="GateTechnologies"/>'in hepsini araştırmış olduğunda geçer. İlk çağın kapısı boştur.
    /// </summary>
    public sealed class EraDefinition
    {
        private readonly TechnologyId[] _gate;

        public EraDefinition(int index, string key, GameDate startDate, TechnologyId[] gateTechnologies)
        {
            Index = index;
            Key = key;
            StartDate = startDate;
            _gate = gateTechnologies ?? new TechnologyId[0];
        }

        public int Index { get; }

        public string Key { get; }

        public GameDate StartDate { get; }

        public IReadOnlyList<TechnologyId> GateTechnologies => _gate;
    }
}
