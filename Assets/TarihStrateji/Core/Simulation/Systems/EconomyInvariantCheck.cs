using System;
using System.Collections.Generic;
using TarihStrateji.Core.Economy;
using TarihStrateji.Core.Economy.Definitions;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P15: ekonomi değişmezleri (negatif stok yok, slot muhasebesi).</summary>
    public sealed class EconomyInvariantCheck : IInvariantCheck
    {
        private readonly EconomyCatalog _catalog;

        public EconomyInvariantCheck(EconomyCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Name => "ekonomi";

        public int Check(GameState state, List<string> violations)
        {
            return EconomyInvariants.Check(state.Economy, _catalog, state.World, violations);
        }
    }
}
