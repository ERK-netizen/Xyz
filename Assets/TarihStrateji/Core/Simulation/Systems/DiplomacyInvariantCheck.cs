using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Diplomacy;
using TarihStrateji.Core.Effects.Events;
using TarihStrateji.Core.Intelligence;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>Diplomasi, istihbarat ve olay/odak değişmezleri (P15).</summary>
    public sealed class DiplomacyInvariantCheck : IInvariantCheck
    {
        private readonly GameContent _content;

        public DiplomacyInvariantCheck(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "diplomasi";

        public int Check(GameState state, List<string> violations)
        {
            return DiplomacyInvariants.Check(state.Diplomacy, state.World, state.Today, violations)
                + IntelligenceInvariants.Check(state.Intelligence, state.World, violations)
                + NarrativeInvariants.Check(state.Narrative, _content.Narrative, violations);
        }
    }
}
