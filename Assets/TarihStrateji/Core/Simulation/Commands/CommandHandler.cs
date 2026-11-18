using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>Tipli emir işleyicisi tabanı.</summary>
    public abstract class CommandHandler<TCommand> : ICommandHandler
        where TCommand : GameCommand
    {
        public abstract string Kind { get; }

        public virtual bool ResolvesImmediately => false;

        public ValidationResult Validate(GameState state, GameCommand command)
        {
            return ValidateTyped(state, Cast(command));
        }

        public void Apply(CommandContext context, GameCommand command)
        {
            ApplyTyped(context, Cast(command));
        }

        public void Write(GameCommand command, CanonicalWriter writer)
        {
            WriteTyped(Cast(command), writer);
        }

        public GameCommand Read(CountryId issuer, CanonicalReader reader)
        {
            return ReadTyped(issuer, reader);
        }

        protected abstract ValidationResult ValidateTyped(GameState state, TCommand command);

        protected abstract void ApplyTyped(CommandContext context, TCommand command);

        protected abstract void WriteTyped(TCommand command, CanonicalWriter writer);

        protected abstract TCommand ReadTyped(CountryId issuer, CanonicalReader reader);

        private TCommand Cast(GameCommand command)
        {
            if (command is TCommand typed)
            {
                return typed;
            }

            throw new ArgumentException("Emir türü işleyiciyle uyuşmuyor: " + Kind, nameof(command));
        }
    }
}
