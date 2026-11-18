namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>Faz sabitleri.</summary>
    public static class SimulationPhases
    {
        public const int Count = 16;

        public static bool IsDefined(SimulationPhase phase) => (int)phase >= 0 && (int)phase < Count;
    }
}
