using System.Collections.Generic;
using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Research.State
{
    /// <summary>
    /// Bir ülkenin araştırma durumu: çağ, araştırılmışlar (tamamlanma tarihiyle), yarım kalan
    /// ilerlemeler (proje değiştirmek ilerlemeyi silmez), güncel proje ve kuyruk.
    /// </summary>
    public sealed class CountryResearch
    {
        internal CountryResearch(CountryId country, int era)
        {
            Country = country;
            Era = era;
        }

        public CountryId Country { get; }

        /// <summary>Ulaşılan çağ dizini.</summary>
        public int Era { get; internal set; }

        public StableIdMap<TechnologyId, GameDate> Researched { get; } = new StableIdMap<TechnologyId, GameDate>();

        public StableIdMap<TechnologyId, Fixed64> Progress { get; } = new StableIdMap<TechnologyId, Fixed64>();

        public TechnologyId Current { get; internal set; }

        internal List<TechnologyId> QueueList { get; } = new List<TechnologyId>();

        public IReadOnlyList<TechnologyId> Queue => QueueList;

        public bool Has(TechnologyId tech) => Researched.ContainsKey(tech);

        public Fixed64 ProgressOf(TechnologyId tech) => Progress.TryGetValue(tech, out Fixed64 p) ? p : Fixed64.Zero;
    }
}
