using System;
using System.Collections.Generic;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Economy.Rules;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P9: inşaat kuyruğu; tamamlanan tesisler gün raporuna not düşülür.</summary>
    public sealed class ConstructionSystem : ISimulationSystem
    {
        public const string CompletedNote = "bilgi.insaat.tamamlandi";

        private readonly EconomyCatalog _catalog;

        public ConstructionSystem(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "ekonomi.insaat";

        public SimulationPhase Phase => SimulationPhase.P9Construction;

        public void Execute(SimulationContext context)
        {
            var completed = new List<FacilityId>();
            ConstructionRules.AdvanceDay(context.State.Economy, _catalog, context.World, context.Effects, context.Today, completed);
            for (int i = 0; i < completed.Count; i++)
            {
                context.Note(CompletedNote + " @ " + completed[i].Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
