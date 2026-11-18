using System;
using System.Collections.Generic;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Research.Definitions;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P5: araştırma ilerlemesi ve çağ geçişi.</summary>
    public sealed class ResearchSystem : ISimulationSystem
    {
        private readonly TechCatalog _catalog;
        private readonly List<string> _notes = new List<string>();

        public ResearchSystem(TechCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "arastirma";

        public SimulationPhase Phase => SimulationPhase.P5Research;

        public void Execute(SimulationContext context)
        {
            _notes.Clear();
            ResearchRules.AdvanceDay(context.State.Research, _catalog, context.Effects, context.Today, _notes);
            for (int i = 0; i < _notes.Count; i++)
            {
                context.Note(_notes[i]);
            }
        }
    }
}
