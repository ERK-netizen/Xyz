using System;
using TarihStrateji.Core.Content;
using TarihStrateji.Core.Domain.Commands;

namespace TarihStrateji.Core.Simulation.Commands.Diplomacy
{
    /// <summary>Tanım verisine (<see cref="GameContent"/>) ihtiyaç duyan diplomasi/olay işleyicilerinin tabanı.</summary>
    public abstract class ContentHandler<TCommand> : CommandHandler<TCommand>
        where TCommand : GameCommand
    {
        protected ContentHandler(GameContent content)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        protected GameContent Content { get; }
    }
}
