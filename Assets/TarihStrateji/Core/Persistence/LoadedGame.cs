using TarihStrateji.Core.Simulation;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>Yüklenmiş oyun: çekirdek + nasıl yüklendiği (kurtarma bildirimi için).</summary>
    public sealed class LoadedGame
    {
        internal LoadedGame(SimulationKernel kernel, SaveLoadStatus status, SaveMetadata metadata)
        {
            Kernel = kernel;
            Status = status;
            Metadata = metadata;
        }

        public SimulationKernel Kernel { get; }

        public SaveLoadStatus Status { get; }

        public SaveMetadata Metadata { get; }
    }
}
