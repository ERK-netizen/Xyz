using System;
using TarihStrateji.Core.Domain;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Simulation;
using TarihStrateji.Core.Simulation.Persistence;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>Çekirdek ↔ kayıt yuvası köprüsü.</summary>
    public static class GameSaveService
    {
        public static ValidationResult Save(SimulationKernel kernel, FileSaveStore store, string slot, string label)
        {
            if (kernel == null || store == null)
            {
                throw new ArgumentNullException(kernel == null ? nameof(kernel) : nameof(store));
            }

            var metadata = new SaveMetadata(label, kernel.CurrentDate, kernel.Mode, kernel.PlayerCountry, GameStateCodec.SchemaVersion);
            return store.Save(slot, metadata, kernel.CaptureSnapshot());
        }

        /// <summary>
        /// Yuvayı yükler ve çekirdeği kurar. Bütünlüğü sağlam ama içeriği geçersiz (değişmezlerden
        /// geçmeyen) aday atlanır; sıradaki (geçici/yedek) denenir.
        /// </summary>
        public static Result<LoadedGame> Load(FileSaveStore store, string slot, SimulationSetup setup)
        {
            if (store == null || setup == null)
            {
                throw new ArgumentNullException(store == null ? nameof(store) : nameof(setup));
            }

            SimulationKernel kernel = null;
            SaveLoadResult result = store.Load(slot, file =>
            {
                Result<SimulationKernel> restored = SimulationKernel.Restore(setup, file.Payload);
                kernel = restored.IsSuccess ? restored.Value : null;
                return restored.IsSuccess;
            });

            if (!result.Succeeded)
            {
                return Result<LoadedGame>.Failure(result.ErrorKey);
            }

            return Result<LoadedGame>.Success(new LoadedGame(kernel, result.Status, result.File.Metadata));
        }
    }
}
