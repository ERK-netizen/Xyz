using TarihStrateji.Core.Domain.Collections;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;

namespace TarihStrateji.Core.Politics.State
{
    /// <summary>
    /// Ülke siyaseti. Parti desteği normalize edilmemiş pozitif ağırlıklardır (pay = ağırlık /
    /// toplam); böylece yuvarlama toplamı 1'den saptıramaz.
    /// </summary>
    public sealed class CountryPolitics
    {
        internal CountryPolitics(CountryId country)
        {
            Country = country;
        }

        public CountryId Country { get; }

        public GovernmentFormId Form { get; internal set; }

        public PartyId RulingParty { get; internal set; }

        /// <summary>[0, 1].</summary>
        public Fixed64 Stability { get; internal set; }

        /// <summary>Seçim yoksa <see cref="GameDate.MinValue"/>.</summary>
        public GameDate NextElection { get; internal set; }

        public StableIdMap<PartyId, Fixed64> PartySupport { get; } = new StableIdMap<PartyId, Fixed64>();

        public StableIdMap<InstitutionId, Fixed64> Institutions { get; } = new StableIdMap<InstitutionId, Fixed64>();

        public StableIdMap<CrisisId, ActiveCrisis> ActiveCrises { get; } = new StableIdMap<CrisisId, ActiveCrisis>();

        /// <summary>Partinin destek payı [0, 1].</summary>
        public Fixed64 SupportShare(PartyId party)
        {
            Fixed64 total = Fixed64.Zero;
            for (int i = 0; i < PartySupport.Count; i++)
            {
                total += PartySupport.ValueAt(i);
            }

            return total.IsPositive && PartySupport.TryGetValue(party, out Fixed64 w) ? Fixed64.MulDiv(w, Fixed64.One, total) : Fixed64.Zero;
        }

        public Fixed64 InstitutionStrength(InstitutionId institution)
        {
            return Institutions.TryGetValue(institution, out Fixed64 s) ? s : Fixed64.Zero;
        }
    }
}
