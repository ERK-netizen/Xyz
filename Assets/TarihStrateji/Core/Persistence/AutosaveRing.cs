using System;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Simulation.Persistence;
using TarihStrateji.Core.Simulation.Phases;
using TarihStrateji.Core.Simulation.State;

namespace TarihStrateji.Core.Persistence
{
    /// <summary>
    /// Dönüşümlü otomatik kayıt halkası (save-systems): <c>otomatik_0 … otomatik_{n-1}</c>.
    /// Elle alınan yuvaları asla ezmez. İlk kullanımda en eski (veya boş) yuvadan başlar.
    /// Disk hatası simülasyonu durdurmaz; <see cref="LastError"/>'a yazılır.
    /// </summary>
    public sealed class AutosaveRing : IAutosaveSink
    {
        public const string Prefix = "otomatik_";

        private readonly FileSaveStore _store;
        private readonly int _size;
        private int _next = -1;

        public AutosaveRing(FileSaveStore store, int size = 3)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            if (size < 1 || size > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _size = size;
        }

        public string LastError { get; private set; }

        public string LastSlot { get; private set; }

        public void Autosave(GameState state, byte[] snapshot)
        {
            if (_next < 0)
            {
                _next = FindStartSlot();
            }

            string slot = SlotName(_next);
            var metadata = new SaveMetadata(slot, state.Today, state.Mode, state.PlayerCountry, GameStateCodec.SchemaVersion);
            ValidationResult result = _store.Save(slot, metadata, snapshot);
            LastError = result.IsValid ? null : result.ErrorKey;
            if (result.IsValid)
            {
                LastSlot = slot;
            }

            _next = (_next + 1) % _size;
        }

        public string SlotName(int index)
        {
            return Prefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private int FindStartSlot()
        {
            int oldest = 0;
            int oldestDay = int.MaxValue;
            for (int i = 0; i < _size; i++)
            {
                SaveLoadResult existing = _store.Load(SlotName(i));
                if (!existing.Succeeded)
                {
                    return i;
                }

                int day = existing.File.Metadata.GameDate.DayNumber;
                if (day < oldestDay)
                {
                    oldestDay = day;
                    oldest = i;
                }
            }

            return oldest;
        }
    }
}
