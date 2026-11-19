using System;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Warfare;

namespace TarihStrateji.Core.Simulation.State
{
    /// <summary>
    /// Warfare'in savaş hâli sorgusu; gerçek kaynak <see cref="DiplomacyState"/>'tir (savaş ilanı ve
    /// barış yalnız diplomasi kurallarıyla değişir). Ayrı durum tutmaz, kayda yazılmaz.
    /// </summary>
    public sealed class DiplomaticHostility : IHostility
    {
        private readonly DiplomacyState _diplomacy;

        public DiplomaticHostility(DiplomacyState diplomacy)
        {
            _diplomacy = diplomacy ?? throw new ArgumentNullException(nameof(diplomacy));
        }

        public bool AtWar(CountryId a, CountryId b) => _diplomacy.AtWar(a, b);
    }
}
