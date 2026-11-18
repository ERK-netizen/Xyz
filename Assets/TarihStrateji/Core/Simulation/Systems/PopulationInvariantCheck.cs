using System;
using System.Collections.Generic;
using TarihStrateji.Core.Population;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>P15: nüfus değişmezleri (toplam = Σ kohort).</summary>
    public sealed class PopulationInvariantCheck : IInvariantCheck
    {
        private readonly DemographyParameters _parameters;

        public PopulationInvariantCheck(DemographyParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public string Name => "nufus";

        public int Check(GameState state, List<string> violations)
        {
            return PopulationInvariants.Check(state.Population, _parameters, violations);
        }
    }
}
