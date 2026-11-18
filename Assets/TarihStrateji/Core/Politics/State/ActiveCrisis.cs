using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Politics.State
{
    /// <summary>Süren kriz: bitiş günü ve uyguladığı etkilerin kimlikleri.</summary>
    public sealed class ActiveCrisis
    {
        internal ActiveCrisis(CrisisId crisis, GameDate startedOn, GameDate lastDay, List<EffectId> effects)
        {
            Crisis = crisis;
            StartedOn = startedOn;
            LastDay = lastDay;
            EffectList = effects;
        }

        public CrisisId Crisis { get; }

        public GameDate StartedOn { get; }

        public GameDate LastDay { get; }

        internal List<EffectId> EffectList { get; }

        public IReadOnlyList<EffectId> Effects => EffectList;
    }
}
