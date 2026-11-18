using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>
    /// Otomatik kayıt hedefi (Persistence modülü uygular; Simulation ona bağımlı değildir).
    /// P15'in en sonunda, değişmezler geçtikten sonra güvenli gün sınırında çağrılır.
    /// </summary>
    public interface IAutosaveSink
    {
        void Autosave(GameState state, byte[] snapshot);
    }
}
