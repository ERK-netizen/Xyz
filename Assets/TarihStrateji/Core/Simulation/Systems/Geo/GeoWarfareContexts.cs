using TarihStrateji.Core.Content;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Warfare.Geo;

namespace TarihStrateji.Core.Simulation.Systems.Geo
{
    /// <summary>Oyun durumundan coğrafi savaş bağlamı kurar.</summary>
    public static class GeoWarfareContexts
    {
        public static GeoWarfareContext From(GameState state, GameContent content) =>
            new GeoWarfareContext(state.World, state.Warfare, content.Warfare, content.GeoWarfare, state.Population, state.Economy, state.Effects, state.Hostility, state.Today);
    }
}
