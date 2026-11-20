using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Geography;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems.Geo;
using TarihStrateji.Core.Warfare;
using TarihStrateji.Core.Warfare.Geo;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Simulation.Commands.Geo
{
    /// <summary>
    /// Coğrafi askerî emir işleyicilerinin tabanı (ADR-011): savaş bağlamı ve rota bulucu kurar. <see cref="TargetRef"/> kanonik olarak yazılır (tür, kimlik, enlem, boylam).
    /// </summary>
    public abstract class GeoMilitaryHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : GameCommand
    {
        protected GeoMilitaryHandler(GameContent content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        protected GameContent Content { get; }

        protected GeoWarfareContext War(GameState state) => GeoWarfareContexts.From(state, Content);

        protected static TransportPathfinder Finder(GameState state) => new TransportPathfinder(state.World.Transport);

        protected sealed override ValidationResult ValidateTyped(GameState state, TCommand command) => ValidateGeo(state, command);

        protected abstract ValidationResult ValidateGeo(GameState state, TCommand command);

        public static void WriteTarget(CanonicalWriter w, TargetRef target) => TargetRefCodec.Write(w, target);

        public static TargetRef ReadTarget(CanonicalReader r) => TargetRefCodec.Read(r);
    }
}
