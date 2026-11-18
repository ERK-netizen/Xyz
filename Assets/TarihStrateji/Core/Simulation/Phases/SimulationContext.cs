using System.Collections.Generic;
using TarihStrateji.Core.Domain.Randomness;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.Effects;
using TarihStrateji.Core.Simulation.State;
using TarihStrateji.Core.World;

namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>
    /// Faz sistemlerine verilen yazma bağlamı. Yalnız SimulationKernel oluşturur (tek yazıcı).
    /// Rastgelelik daima <see cref="Rng"/> ile, günün anahtarıyla alınır.
    /// </summary>
    public sealed class SimulationContext
    {
        private readonly List<string> _notes;

        internal SimulationContext(GameState state, List<string> notes)
        {
            State = state;
            _notes = notes;
        }

        public GameState State { get; }

        public GameDate Today => State.Today;

        public WorldMap World => State.World;

        public EffectEngine Effects => State.Effects;

        public ResolutionLedger Ledger => State.Ledger;

        public SimulationPhase CurrentPhase { get; internal set; }

        public RngStream Rng(RngSubsystem subsystem, ulong eventId, ulong entityId)
        {
            return State.Rng(subsystem, eventId, entityId);
        }

        /// <summary>Gün raporuna not (yerelleştirme anahtarı + bağlam).</summary>
        public void Note(string note)
        {
            _notes.Add(note);
        }
    }
}
