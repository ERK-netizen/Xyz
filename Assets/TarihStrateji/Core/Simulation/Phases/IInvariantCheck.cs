using System.Collections.Generic;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>P15'te çalışan değişmez denetimi; ihlal simülasyonu durdurur.</summary>
    public interface IInvariantCheck
    {
        string Name { get; }

        /// <summary>İhlalleri ekler ve eklenen sayıyı döner.</summary>
        int Check(GameState state, List<string> violations);
    }
}
