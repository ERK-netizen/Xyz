namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>
    /// Günlük fazlardan birinde çalışan sistem. Aynı fazdaki sistemler kayıt sırasıyla çalışır.
    /// Sistem durumu yalnız <see cref="SimulationContext"/> üzerinden değiştirir; kendi içinde
    /// kalıcı durum tutmaz (tüm kalıcı durum <c>GameState</c>'tedir ve kayda girer).
    /// </summary>
    public interface ISimulationSystem
    {
        /// <summary>Kalıcı, benzersiz ad (rapor ve hata iletileri için).</summary>
        string Name { get; }

        SimulationPhase Phase { get; }

        void Execute(SimulationContext context);
    }
}
