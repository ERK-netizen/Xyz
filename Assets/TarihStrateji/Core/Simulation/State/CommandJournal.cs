using System;
using System.Collections.Generic;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>
    /// Bir sonraki günlük adımda P1'de uygulanacak emirler (CommandId sırasında). Anında
    /// çözülen emirler buraya girmez; yalnız çözüm defterine (<see cref="ResolutionLedger"/>) girer.
    /// </summary>
    public sealed class CommandJournal
    {
        private readonly List<JournalEntry> _pending = new List<JournalEntry>();

        public IReadOnlyList<JournalEntry> Pending => _pending;

        internal void AddPending(JournalEntry entry)
        {
            if (entry.IsResolved)
            {
                throw new ArgumentException("Çözülmüş emir bekleyen listeye eklenemez.", nameof(entry));
            }

            if (_pending.Count > 0 && _pending[_pending.Count - 1].Id >= entry.Id)
            {
                throw new InvalidOperationException("Emir kimlikleri artan sırada eklenmeli.");
            }

            _pending.Add(entry);
        }

        internal void ClearPending()
        {
            _pending.Clear();
        }
    }
}
