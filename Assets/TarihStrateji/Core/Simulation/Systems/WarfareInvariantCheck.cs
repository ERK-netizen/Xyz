using System;
using System.Collections.Generic;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Simulation.Systems.Geo;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P15: savaş değişmezleri (birlik, filo, kanat konumları ve değerleri).</summary>
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
            return WarfareInvariants.Check(GeoWarfareContexts.From(state, _content), violations);
        }
    }
}
