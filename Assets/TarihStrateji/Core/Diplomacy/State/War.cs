using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>
    /// İki ülke arasındaki savaş (savaşlar çiftler halinde tutulur; ittifakla katılan her ülke
    /// saldırganla ayrı bir çift oluşturur ve ayrı barış yapar). <see cref="Goals"/>: saldırganın
    /// savaş hedefi olarak talep ettiği, savunana ait bölgeler.
    /// </summary>
    public sealed class War
    {
        internal War(WarId id, CountryId attacker, CountryId defender, GameDate start, WarId calledBy)
        {
            Id = id;
            Attacker = attacker;
            Defender = defender;
            Start = start;
            CalledBy = calledBy;
        }

        public WarId Id { get; }

        public CountryId Attacker { get; }

        public CountryId Defender { get; }

        public GameDate Start { get; }

        /// <summary>İttifak çağrısıyla açıldıysa çağıran savaş; asıl savaşta <see cref="WarId.None"/>.</summary>
        public WarId CalledBy { get; }

        internal List<RegionId> GoalList { get; } = new List<RegionId>();

        public IReadOnlyList<RegionId> Goals => GoalList;

        public bool Involves(CountryId country) => Attacker == country || Defender == country;

        public CountryId Opponent(CountryId country) => Attacker == country ? Defender : Attacker;
    }
}
