using System;
using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>
    /// Gün içi anında çözülen taktik eylemlerin defteri (CLAUDE.md §2.5). Son günlük adımdan
    /// bu yana çözülenler burada tutulur; P13 ve diğer fazlar <see cref="Contains"/> ile ikinci
    /// kez uygulamaz. Defter P15 sonunda boşaltılır ve kayda yazılır.
    /// </summary>
    public sealed class ResolutionLedger
    {
        private readonly List<ResolutionId> _resolved = new List<ResolutionId>();

        public int Count => _resolved.Count;

        public IReadOnlyList<ResolutionId> Resolved => _resolved;

        public bool Contains(ResolutionId id)
        {
            return _resolved.BinarySearch(id) >= 0;
        }

        internal void Record(ResolutionId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Geçersiz çözüm kimliği.", nameof(id));
            }

            int index = _resolved.BinarySearch(id);
            if (index >= 0)
            {
                throw new InvalidOperationException("Çözüm kimliği zaten kayıtlı.");
            }

            _resolved.Insert(~index, id);
        }

        internal void Clear()
        {
            _resolved.Clear();
        }
    }
}
