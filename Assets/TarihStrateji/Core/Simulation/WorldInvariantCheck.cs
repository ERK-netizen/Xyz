using System.Collections.Generic;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation
{
    /// <summary>Dünya değişmezleri (her zaman etkin, P15).</summary>
    internal sealed class WorldInvariantCheck : IInvariantCheck
    {
        public string Name => "dunya";

        public int Check(GameState state, List<string> violations)
        {
            return state.IsGeographic ? GeoWorldInvariants.Check(state.Geo, violations) : WorldInvariants.Check(state.World, violations);
        }
    }
}
