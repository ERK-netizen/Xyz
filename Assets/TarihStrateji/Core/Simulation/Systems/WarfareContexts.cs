using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Systems
{
    /// <summary>Oyun durumundan savaş bağlamı kurar.</summary>
    public static class WarfareContexts
    {
        public static WarfareContext From(GameState state, GameContent content)
        {
            return new WarfareContext(state.World, state.Warfare, content.Warfare, state.Population, state.Economy, state.Effects, state.Hostility, state.Today);
        }
    }
}
