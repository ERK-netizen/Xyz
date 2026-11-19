using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P15: kara birliği değişmezleri.</summary>
    public sealed class WarfareInvariantCheck : IInvariantCheck
    {
        private readonly GameContent _content;

        public WarfareInvariantCheck(GameContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public string Name => "savas";

        public int Check(GameState state, List<string> violations)
        {
            return WarfareInvariants.Check(WarfareContexts.From(state, _content), violations);
        }
    }
}
