using System;
using System.Collections.Generic;

namespace TarihStrateji.Core.Simulation.Commands
{
    /// <summary>Emir türü → işleyici. Kayıt sırası anlamsızdır; arama sıralı (ordinal) anahtarla yapılır.</summary>
    public sealed class CommandRegistry
    {
        private readonly Dictionary<string, ICommandHandler> _handlers = new Dictionary<string, ICommandHandler>(StringComparer.Ordinal);

        public int Count => _handlers.Count;

        public void Register(ICommandHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            if (string.IsNullOrEmpty(handler.Kind) || _handlers.ContainsKey(handler.Kind))
            {
                throw new ArgumentException(SimulationErrorKeys.EmirTuruTekrar + ": " + handler.Kind, nameof(handler));
            }

            _handlers.Add(handler.Kind, handler);
        }

        public bool TryGet(string kind, out ICommandHandler handler)
        {
            if (kind == null)
            {
                handler = null;
                return false;
            }

            return _handlers.TryGetValue(kind, out handler);
        }
    }
}
