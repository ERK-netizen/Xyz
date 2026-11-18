using System;
using System.Collections.Generic;
using TarihStrateji.Core.Characters;
using TarihStrateji.Core.Politics;
using TarihStrateji.Core.Research;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P15: araştırma, siyaset ve karakter değişmezleri.</summary>
    public sealed class StatecraftInvariantCheck : IInvariantCheck
    {
        private readonly GameContent _content;

        public StatecraftInvariantCheck(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "devlet";

        public int Check(GameState state, List<string> violations)
        {
            return ResearchInvariants.Check(state.Research, _content.Technology, violations)
                + PoliticsInvariants.Check(state.Politics, _content.Politics, violations)
                + CharacterInvariants.Check(state.Characters, _content.Characters, state.Today, violations);
        }
    }
}
