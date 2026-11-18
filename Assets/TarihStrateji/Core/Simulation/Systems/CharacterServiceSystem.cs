using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Characters.Definitions;
using TarihStrateji.Core.Simulation.Phases;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P4 (siyasetten önce): hizmet penceresi biten karakterleri görevden alır.</summary>
    public sealed class CharacterServiceSystem : ISimulationSystem
    {
        private readonly CharacterCatalog _catalog;
        private readonly List<string> _notes = new List<string>();

        public CharacterServiceSystem(CharacterCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "karakter.hizmet";

        public SimulationPhase Phase => SimulationPhase.P4Politics;

        public void Execute(SimulationContext context)
        {
            _notes.Clear();
            CharacterRules.ExpireDay(context.State.Characters, _catalog, context.Effects, context.Today, _notes);
            for (int i = 0; i < _notes.Count; i++)
            {
                context.Note(_notes[i]);
            }
        }
    }
}
