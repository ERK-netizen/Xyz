using System;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Domain.Commands
{
    /// <summary>
    /// <see cref="IGameCommandGateway.Submit"/> sonucu. Kabul edilen emir bir
    /// <see cref="CommandId"/> alır ve <see cref="Date"/> gününün günlüğüne işlenmiştir.
    /// </summary>
    [Serializable]
    public readonly struct CommandReceipt
    {
        private CommandReceipt(CommandId commandId, GameDate date, ValidationResult validation)
        {
            CommandId = commandId;
            Date = date;
            Validation = validation;
        }

        public CommandId CommandId { get; }

        public GameDate Date { get; }

        public ValidationResult Validation { get; }

        public bool Accepted => Validation.IsValid;

        public static CommandReceipt Accept(CommandId commandId, GameDate date)
        {
            if (!commandId.IsValid)
            {
                throw new ArgumentException(DomainErrorKeys.KimlikGecersiz, nameof(commandId));
            }

            return new CommandReceipt(commandId, date, ValidationResult.Ok);
        }

        public static CommandReceipt Reject(ValidationResult validation, GameDate date)
        {
            if (validation.IsValid)
            {
                throw new ArgumentException("Reddedilen emir geçersiz bir doğrulama sonucu gerektirir.", nameof(validation));
            }

            return new CommandReceipt(CommandId.None, date, validation);
        }
    }
}
