using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.Simulation.Systems;
using TarihStrateji.Core.Warfare.Rules;

namespace TarihStrateji.Core.Simulation.Commands.Military
{
    /// <summary>Kara harbi emir işleyicileri için ortak taban (savaş bağlamı kurar).</summary>
    public abstract class MilitaryHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : GameCommand
    {
        protected MilitaryHandler(GameContent content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        protected GameContent Content { get; }

        protected WarfareContext War(GameState state) => WarfareContexts.From(state, Content);
    }
}
