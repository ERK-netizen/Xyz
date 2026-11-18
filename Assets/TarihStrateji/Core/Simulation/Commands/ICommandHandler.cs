using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Serialization;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>
    /// Bir emir türünün kuralları ve kayıt biçimi. <see cref="Validate"/> durumu değiştirmez;
    /// <see cref="Apply"/> yalnız geçerli emirle ve yalnız SimulationKernel tarafından çağrılır.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary><see cref="GameCommand.Kind"/> ile aynı, kalıcı tür anahtarı.</summary>
        string Kind { get; }

        /// <summary>true: gün içinde anında çözülür (taktik saldırı) ve ResolutionId alır.</summary>
        bool ResolvesImmediately { get; }

        ValidationResult Validate(GameState state, GameCommand command);

        void Apply(CommandContext context, GameCommand command);

        void Write(GameCommand command, CanonicalWriter writer);

        GameCommand Read(CountryId issuer, CanonicalReader reader);
    }
}
