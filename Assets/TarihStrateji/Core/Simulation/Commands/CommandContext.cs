using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>
    /// Emir uygulama bağlamı. Rastgelelik anahtarı emrin kimliğine bağlıdır:
    /// <c>(worldSeed, Tactical/Kernel, gün, commandId, issuer)</c>.
    /// </summary>
    public sealed class CommandContext
    {
        internal CommandContext(GameState state, CommandId commandId, ResolutionId resolution, RngStream rng)
        {
            State = state;
            CommandId = commandId;
            Resolution = resolution;
            Rng = rng;
        }

        public GameState State { get; }

        public CommandId CommandId { get; }

        /// <summary>Anında çözümde geçerli; P1 uygulamasında <see cref="ResolutionId.None"/>.</summary>
        public ResolutionId Resolution { get; }

        public RngStream Rng;
    }
}
